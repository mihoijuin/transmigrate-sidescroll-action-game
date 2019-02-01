﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    // 移動用
    public float updownSpeed;
    public float slideSpeed;
    public float playerRadius = 0.8f;

    public float stageMoveSpeed;

    // 揺らぎ
    public float playerScaleOffset = 0.1f;
    public float playerMinScale = 0.9f;
    public float fluctuationSpeed = 0.15f;
    float scaleCount;

    // 色変化
    [SerializeField]
    private float colorChangeAmount;

    // 死ぬ時
    public float flashSpeed;
    public float flashtimes;

    // ゴール判定
    public float goalSpeed;

    BGMDirector bgmDirector;
    SEDirector seDirector;


    ItemDirector itemDirector;
    StageDirector stageDirector;

    public Rigidbody2D playerRigid;
    Renderer playerRenderer;

    GameObject playerAura;
    Renderer auraRenderer;

    Color originColor;
    float originscale;

	void Start () {
        playerRigid = GetComponent<Rigidbody2D>();
        playerRenderer = GetComponent<Renderer>();
        playerAura = transform.GetChild(0).gameObject;
        auraRenderer = playerAura.GetComponent<Renderer>();

        itemDirector = FindObjectOfType<ItemDirector>();
        stageDirector = FindObjectOfType<StageDirector>();
        bgmDirector = FindObjectOfType<BGMDirector>();
        seDirector = FindObjectOfType<SEDirector>();

        originColor = playerRenderer.material.color;
        originscale = transform.localScale.x;

	}

    private void Update()
    {
        if (stageDirector.stageState != StageDirector.STAGESTATE.NONE)
        {
            // 炎のようにゆらゆら
            scaleCount = Mathf.PingPong(Time.time * fluctuationSpeed, playerScaleOffset) + playerMinScale;
            playerAura.transform.localScale = new Vector3(scaleCount, scaleCount, 1);
        }
    }


    void FixedUpdate () {
        // 基本動作
        switch (stageDirector.stageState)
        {
            case StageDirector.STAGESTATE.INSTAGE:
                // 徐々に右に動く
                playerRigid.velocity = Vector2.right * slideSpeed;
                break;
            case StageDirector.STAGESTATE.MOVE:
                // 移動
                playerRigid.velocity = Vector2.right * stageMoveSpeed;
                break;
            case StageDirector.STAGESTATE.NONE:
                playerRigid.velocity = Vector2.zero;
                break;
        }

        // 上下移動
        if (Input.GetMouseButton(0) && stageDirector.stageState == StageDirector.STAGESTATE.INSTAGE)
        {
            Vector3 inputPos = Input.mousePosition;
            Vector3 screenCenterPos = Camera.main.ViewportToScreenPoint(new Vector2(0, 0.5f));

            if(inputPos.y > screenCenterPos.y)
            {
                MoveUp();
            }
            else
            {
                MoveDown();
            }
        }              

    }

    private void MoveUp()
    {
        Vector2 newPos = playerRigid.position + Vector2.up * updownSpeed;
        Vector2 min = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 max = Camera.main.ViewportToWorldPoint(new Vector2(1, 1));

        // 画面外には出ないようにする
        newPos.y = Mathf.Clamp(newPos.y, min.y, max.y - playerRadius);

        playerRigid.MovePosition(newPos);
    }

    private void MoveDown()
    {
        Vector2 newPos = playerRigid.position + Vector2.down * updownSpeed;
        Vector2 min = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 max = Camera.main.ViewportToWorldPoint(new Vector2(1, 1));

        // 画面外には出ないようにする
        newPos.y = Mathf.Clamp(newPos.y, min.y + playerRadius, max.y);

        playerRigid.MovePosition(newPos);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        switch (collision.tag)
        {
            case "GoodItem":
                // ポイントを増加
                itemDirector.CountUpPoint();
                itemDirector.SwitchState();

                // SEならす
                seDirector.PlaySE(SEDirector.SE.GOODITEM);

                // プレイヤーの見た目状態を更新
                StrengthenPlayerVisual();

                Destroy(collision.gameObject);
                break;
            case "BadItem":
                // ポイント現象
                itemDirector.CountDownPoint();
                itemDirector.SwitchState();

                // SEならす
                seDirector.PlaySE(SEDirector.SE.BADITEM);

                // プレイヤーの見た目を更新
                WeakenPlayerVisiual();

                Destroy(collision.gameObject);
                break;
            case "Switch":
                StartCoroutine(PushSwitch(collision.gameObject));
                break;
            case "Goal":
                StartCoroutine(EnterGoal(collision.gameObject));
                break;
            default:
                Debug.Log("unknown collider");
                break;
       }

    }

    void StrengthenPlayerVisual()
    {
        Color currentColor = playerRenderer.material.color;

        // 色が変更されていたら元に戻す
        if(originColor != currentColor)
        {
            playerRenderer.material.color = new Color(currentColor.r + colorChangeAmount, currentColor.g, currentColor.b);
            auraRenderer.material.color = new Color(currentColor.r + colorChangeAmount, currentColor.g, currentColor.b);
        }
        else
        {
            // オーラが大きくなっていく
            // TODO 揺らぎに変えたら動かし方変える
            playerMinScale += 0.1f;
        }

    }

    void WeakenPlayerVisiual()
    {
        Color currentColor = playerRenderer.material.color;

        // オーラ大きさが変わっていたら元に戻す
        if(playerMinScale > originscale)
        {
            playerMinScale -= 0.1f;
        }
        else
        {
            // 色を変更
            playerRenderer.material.color = new Color(currentColor.r - colorChangeAmount, currentColor.g, currentColor.b);
            auraRenderer.material.color = new Color(currentColor.r - colorChangeAmount, currentColor.g, currentColor.b);
        }

    }


    public IEnumerator Die()
    {
        yield return new WaitForSeconds(1f);

        for (int i=0; i<flashtimes; i++)
        {
            yield return new WaitForSeconds(flashSpeed);
            GetComponent<SpriteRenderer>().enabled = false;
            
            yield return new WaitForSeconds(flashSpeed);
            GetComponent<SpriteRenderer>().enabled = true;
        }

        yield return new WaitForSeconds(1f);
        Destroy(gameObject);

        yield break;
    }


    IEnumerator PushSwitch(GameObject goalSwitch)
    {

        bgmDirector.StopBGM();
        stageDirector.stageState = StageDirector.STAGESTATE.NONE;

        float targetPosX;
        float targetPosY;

        yield return new WaitForSeconds(0.5f);

        while ((goalSwitch.transform.position - transform.position).magnitude > 0.05f)
        {

            targetPosX = Mathf.SmoothStep(transform.position.x, goalSwitch.transform.position.x, goalSpeed);
            targetPosY = Mathf.SmoothStep(transform.position.y, goalSwitch.transform.position.y, goalSpeed);

            transform.position = new Vector3(targetPosX, targetPosY, transform.position.z);

            yield return new WaitForSeconds(0.01f);
        }

        seDirector.PlaySE(SEDirector.SE.SWITCH);
        stageDirector.DestroyStage(goalSwitch.transform.parent.gameObject);

        yield return new WaitForSeconds(0.5f);

        bgmDirector.PlayStageMusic();
        stageDirector.stageState = StageDirector.STAGESTATE.MOVE;

        yield break;
    }

    IEnumerator EnterGoal(GameObject goal)
    {
        bgmDirector.StopBGM();
        stageDirector.stageState = StageDirector.STAGESTATE.NONE;

        float targetPosX;
        float targetPosY;

        yield return new WaitForSeconds(0.5f);

        while ((goal.transform.position - transform.position).magnitude > 0.05f)
        {

            targetPosX = Mathf.SmoothStep(transform.position.x, goal.transform.position.x, goalSpeed);
            targetPosY = Mathf.SmoothStep(transform.position.y, goal.transform.position.y, goalSpeed);

            transform.position = new Vector3(targetPosX, targetPosY, transform.position.z);

            yield return new WaitForSeconds(0.01f);
        }

        seDirector.PlaySE(SEDirector.SE.SWITCH);
        stageDirector.DestroyStage(goal.transform.parent.gameObject);

        yield return new WaitForSeconds(0.5f);
        stageDirector.EndGame();
    }
}
