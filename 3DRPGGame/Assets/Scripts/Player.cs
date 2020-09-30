using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Player : MonoBehaviour
{
    #region 欄位：基本資料
    [Header("移動速度"), Range(0, 1000)]
    public float speed = 5;
    [Header("旋轉速度"), Range(0, 1000)]
    public float turn = 5;
    [Header("攻擊力"), Range(0, 500)]
    public float attack = 20;
    [Header("血量"), Range(0, 500)]
    public float hp = 250;
    [Header("魔力"), Range(0, 500)]
    public float mp = 50;
    [Header("音效")]
    public AudioClip soundProp;
    public AudioClip soundMeteor;
    public AudioClip soundAttack;
    public AudioClip soundHit;
    [Header("任務數量")]
    public Text textMission;
    [Header("吧條")]
    public Image barHp;
    public Image barMp;
    public Image barExp;
    public Text textLv;
    [Header("技能：流星雨")]
    public GameObject rock;
    public Transform pointRock;
    public float costRock = 20;
    public float damageRock = 100;
    [Header("回魔量 / 每秒")]
    public float restoreMp = 5;
    [Header("回血量 / 每秒")]
    public float restoreHp = 10;
    [Header("遊戲結束畫面")]
    public CanvasGroup final;
    public Text textFinalTitle;

    private int lv = 1;              // 等級
    private float exp;               // 目前經驗值
    private float maxExp = 100;      // 最大經驗值 (升級所需要)

    private int count;
    private float maxHp;
    private float maxMp;
    private float[] exps;            // 浮點數陣列

    // 在屬性 (Inspector) 面板上隱藏
    [HideInInspector]
    /// <summary>
    /// 是否停止
    /// </summary>
    public bool stop;

    private Animator ani;
    private Rigidbody rig;
    private AudioSource aud;
    private NPC npc;
    /// <summary>
    /// 攝影機根物件
    /// </summary>
    private Transform cam;
    #endregion

    #region 方法：功能
    private void Move()
    {
        float h = Input.GetAxis("Horizontal");      // A D & 左 右：A 左 -1，D 右 1，沒按 0
        float v = Input.GetAxis("Vertical");        // S W & 下 上：S 下 -1，W 上 1，沒按 0

        // Vector3 pos = new Vector3(h, 0, v);                          // 要移動的座標 - 世界座標版本
        // Vector3 pos = transform.forward * v + transform.right * h;   // 要移動的座標 - 區域座標版本
        Vector3 pos = cam.forward * v + cam.right * h;                  // 要移動的座標 - 攝影機的區域座標版本

        // 剛體.移動作標(本身的座標 + 要移動的座標 * 速度 * 1/50)
        rig.MovePosition(transform.position + pos * speed * Time.fixedDeltaTime);
        // 動畫.設定浮點數("參數名稱"，前後 + 左右 的 絕對值)
        ani.SetFloat("移動", Mathf.Abs(v) + Mathf.Abs(h));

        // 如果有控制再轉向：避免沒移動時轉回原點
        if (h != 0 || v != 0)
        {
            pos.y = 0;                                                                                      // 鎖定 Y 軸 避免吃土
            Quaternion angle = Quaternion.LookRotation(pos);                                                // 將前往的座標資訊轉為角度
            transform.rotation = Quaternion.Slerp(transform.rotation, angle, turn * Time.fixedDeltaTime);   // 角度插值
        }
    }

    /// <summary>
    /// 吃道具
    /// </summary>
    private void EatProp()
    {
        count++;                                                                // 遞增
        textMission.text = "骷髏頭：" + count + " / " + npc.data.count;          // 更新介面

        // 如果 數量 等於 NPC 需求數量 就 呼叫 NPC 結束任務
        if (count == npc.data.count) npc.Finish();
    }

    /// <summary>
    /// 受傷
    /// </summary>
    /// <param name="damage">傷害值</param>
    /// <param name="direction">擊退方向</param>
    public void Hit(float damage, Transform direction)
    {
        aud.PlayOneShot(soundHit);
        hp -= damage;
        rig.AddForce(direction.forward * 100 + direction.up * 150);

        barHp.fillAmount = hp / maxHp;
        ani.SetTrigger("受傷觸發");

        if (hp <= 0) Dead();            // 如果 血量 <= 0 就 死亡
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void Dead()
    {
        // this.enabled = false; // 第一種寫法，this 此腳本
        enabled = false;                                        // 此腳本.啟動 = 否
        ani.SetBool("死亡開關", true);                           // 死亡動畫

        StartCoroutine(ShowFinal());                            // 啟動結束畫面協成
    }

    /// <summary>
    /// 顯示結束畫面
    /// </summary>
    private IEnumerator ShowFinal()
    {
        yield return new WaitForSeconds(0.5f);              // 等待 0.5 秒

        textFinalTitle.text = "任務失敗，請重新挑戰";         // 更新標題

        while (final.alpha < 1)                             // 當 透明度 小於 1 時累加
        {
            final.alpha += 0.5f * Time.deltaTime;
            yield return null;
        }

        Cursor.visible = true;                              // 顯示滑鼠
        final.interactable = true;                          // 結束畫面可互動
        final.blocksRaycasts = true;                        // 開啟滑鼠阻擋
    }

    /// <summary>
    /// 攻擊
    /// </summary>
    private void Attack()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            aud.PlayOneShot(soundAttack);
            ani.SetTrigger("攻擊觸發");
        }
    }

    /// <summary>
    /// 經驗值
    /// </summary>
    /// <param name="expGet">取得的經驗值</param>
    public void Exp(float expGet)
    {
        exp += expGet;                          // 經驗值累加
        barExp.fillAmount = exp / maxExp;       // 更新經驗值吧條

        while (exp >= maxExp) LevelUp();        // 當 目前經驗值 >= 最大經驗值 就 升級
    }

    /// <summary>
    /// 升級
    /// </summary>
    private void LevelUp()
    {
        lv++;                           // 等級遞增
        textLv.text = "Lv " + lv;       // 更新等級介面

        // 升級後數值提升
        maxHp += 20;
        maxMp += 5;
        attack += 10;

        // 升級後血量魔力全滿
        hp = maxHp;
        mp = maxMp;

        barHp.fillAmount = 1;
        barMp.fillAmount = 1;

        exp -= maxExp;                      // 把多餘的經驗值補回去 120 -= 100 (20)
        maxExp = exps[lv - 1];              // 最大經驗值 = 經驗值需求[等級 - 1]
        barExp.fillAmount = exp / maxExp;   // 更新經驗值長度 = 目前經驗值 / 最大經驗值
    }

    /// <summary>
    /// 技能：流星雨
    /// </summary>
    private void SkillRock()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1) && mp >= costRock)                     // 如果 按下 右鍵 並且 魔力 >= 技能消耗
        {
            aud.PlayOneShot(soundMeteor);
            ani.SetTrigger("技能觸發");                                              // 播放動畫
            Instantiate(rock, pointRock.position, pointRock.rotation);              // 生成(物件，座標，角度)
            mp -= costRock;                                                         // 扣除消耗量
            barMp.fillAmount = mp / maxMp;                                          // 更新 魔力 吧條
        }
    }

    private void RestoreMp()
    {
        mp += restoreMp * Time.deltaTime;           // 每秒恢復
        mp = Mathf.Clamp(mp, 0, maxMp);             // 夾住數值(數值，0，最大值)
        barMp.fillAmount = mp / maxMp;              // 更新介面
    }

    // 參數前方加入 ref 會變成傳址，將整筆資料傳過來，可以改變傳過來的資料
    /// <summary>
    /// 恢復數值
    /// </summary>
    /// <param name="value">要恢復的值</param>
    /// <param name="restore">每秒恢復多少</param>
    /// <param name="max">要恢復的值最大值</param>
    /// <param name="bar">要更新的吧條</param>
    private void Restore(ref float value, float restore, float max, Image bar)
    {
        value += restore * Time.deltaTime;
        value = Mathf.Clamp(value, 0, max);
        bar.fillAmount = value / max;
    }
    #endregion

    #region 事件：入口
    /// <summary>
    /// 喚醒：會在 Start 前執行一次
    /// </summary>
    private void Awake()
    {
        // 取得元件<泛型>() - 泛型 任何類型
        ani = GetComponent<Animator>();
        rig = GetComponent<Rigidbody>();
        aud = GetComponent<AudioSource>();

        cam = GameObject.Find("攝影機根物件").transform;     // 遊戲物件.尋找("物件名稱") - 建議不要在 Update 內使用

        npc = FindObjectOfType<NPC>();                      // 取得 NPC

        maxHp = hp;
        maxMp = mp;

        // 經驗值需求 總共有 99 筆
        exps = new float[99];

        // 迴圈執行每一筆經驗值需求 = 100 * 等級
        // 陣列.Length 為陣列的數量，此範例為 99
        for (int i = 0; i < exps.Length; i++) exps[i] = 100 * (i + 1);
    }

    /// <summary>
    /// 固定更新：固定 50 FPS
    /// 有物理運動在這裡呼叫 Rigidbody
    /// </summary>
    private void FixedUpdate()
    {
        if (stop) return;           // 如果 停止 就跳出

        // 如果 第一層的動畫名稱 是 攻擊 或者 技能 就跳出
        if (ani.GetCurrentAnimatorStateInfo(0).IsName("攻擊") || ani.GetCurrentAnimatorStateInfo(0).IsName("技能")) return;

        Move();
    }

    private void Update()
    {
        Attack();                                   // 攻擊
        SkillRock();                                // 施放技能
        Restore(ref hp, restoreHp, maxHp, barHp);   // 恢復血量 有 ref 的參數在呼叫時也要添加 ref 關鍵字
        Restore(ref mp, restoreMp, maxMp, barMp);   // 恢復魔力
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "骷髏頭")
        {
            aud.PlayOneShot(soundProp);             // 播放音效
            Destroy(collision.gameObject);          // 刪除道具
            EatProp();                              // 呼叫吃道具
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "怪物")
        {
            other.GetComponent<Enemy>().Hit(attack, transform);
        }
    }
    #endregion
}
