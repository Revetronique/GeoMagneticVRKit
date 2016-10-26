using UnityEngine;
using System.Collections;

public class DeviceDirection : MonoBehaviour {

    /// <summary>
    /// 回転スピード
    /// </summary>
    [Range(1, 30), Tooltip("方位の変化する速度")]
    public float rotSpeed = 5;

    //最後にコンパスを更新した時刻
    double lastCompassTime;

    Quaternion targetDirection;

    //デバイスのピッチ角、ロール角
    float rolling, pitching;
    
    /// <summary>
    /// デバイスのロール角(左右の傾き)
    /// </summary>
    public float Rolling { get { return rolling; } }
    
    /// <summary>
    /// デバイスのピッチ角(上下の傾き)
    /// </summary>
    public float Pitching { get { return pitching; } }

    /// <summary>
    /// 地磁気コンパスの生データベクトルをデバイスの向きに合わせて変換
    /// http://vr-cto.hateblo.jp/entry/2016/05/02/070000
    /// </summary>
    public static Vector3 CompassRawVector
    {
        get
        {
            Vector3 ret = Input.compass.rawVector;

            switch (Input.deviceOrientation)
            {
                case DeviceOrientation.LandscapeLeft:
                    ret = new Vector3(-ret.y, ret.x, ret.z);
                    break;
                case DeviceOrientation.LandscapeRight:
                    ret = new Vector3(ret.y, -ret.x, ret.z);
                    break;
                case DeviceOrientation.PortraitUpsideDown:
                    ret = new Vector3(-ret.x, -ret.y, ret.z);
                    break;
            }

            return ret;
        }
    }

	// Use this for initialization
	void Start () {
        //地磁気の有効化
        Input.compass.enabled = true;
        
        //デバイスの傾き(ピッチ、ロール角)を求める
        GetAccGrad(out rolling, out pitching);

        //ヨー角(Y軸回転)は地磁気から求める
        float yawing = GetFlatNorth().y;

        //カメラの方位を決定
        transform.rotation = Quaternion.Euler(pitching, yawing, rolling);
    }

    // Update is called once per frame
    void Update () {
        //コンパスが更新されたときのみ
        if(Input.compass.timestamp > lastCompassTime)
        {
            //時刻を更新
            lastCompassTime = Input.compass.timestamp;

            //デバイスの傾き(ピッチ、ロール角)を求める
            GetAccGrad(out rolling, out pitching);

            //ヨー角(Y軸回転)は地磁気から求める
            float yawing = GetFlatNorth().y;

            //オブジェクトの目標角度を変更する
            targetDirection = Quaternion.Euler(pitching, yawing, rolling);
        }

        //方位をスムーズに変異させる
        transform.rotation = Quaternion.Slerp(transform.rotation, targetDirection, Time.deltaTime * rotSpeed);
    }

    /// <summary>
    /// デジタルコンパスの生データから3D方位を求める
    /// </summary>
    /// <returns>方位ベクトル(Vector3)</returns>
    public static Vector3 GetFlatNorth()
    {
        //地磁気を有効にしておく
        if (!Input.compass.enabled) Input.compass.enabled = true;
        
        Quaternion accOrientation = changeAxis(Quaternion.Euler(Input.acceleration));
        Vector3 gravity = Input.acceleration.normalized;
        Vector3 flatNorth = CompassRawVector - Vector3.Dot(gravity, CompassRawVector) * gravity;

        Quaternion compassOrientation = changeAxis(Quaternion.Inverse(Quaternion.LookRotation(flatNorth, -gravity)));

        //+zを北にするためQuaternion.Euler(0, 0, 180)を入れる
        Quaternion targetCorrection = compassOrientation * Quaternion.Inverse(accOrientation) * Quaternion.Euler(0, 0, 180);

        //計算結果にエラーが出た場合、0を表示
        if (isNan(targetCorrection))
        {
            return Vector3.zero;
        }
        else
        {
            return targetCorrection.eulerAngles;
        }
    }

    /// <summary>
    /// デバイスの傾きを重力加速度から求める
    /// </summary>
    /// <param name="roll">デバイスのロール角</param>
    /// <param name="pitch">デバイスのピッチ角</param>
    public static void GetAccGrad(out float roll, out float pitch)
    {
        //加速度の正規化ベクトル
        var acc = Input.acceleration.normalized;

        //Y軸に対するX軸の割合で左右の傾き
        roll = Mathf.Rad2Deg * -Mathf.Atan2(acc.x, -acc.y);

        //Y軸に対するZ軸の割合で前後の傾き
        pitch = Mathf.Rad2Deg * -Mathf.Atan2(acc.z, -acc.y);
    }

    /// <summary>
    /// Quaternionの各要素がNaNもしくはInfinityかどうかチェック
    /// </summary>
    /// <param name="q">地磁気の生データ</param>
    /// <returns>NanかInfinityの値があるか</returns>
    static bool isNan(Quaternion q)
    {
        return float.IsNaN(q.x) || float.IsNaN(q.y) || 
               float.IsNaN(q.z) || float.IsNaN(q.w) ||
               float.IsInfinity(q.x) || float.IsInfinity(q.y) || 
               float.IsInfinity(q.z) || float.IsInfinity(q.w);
    }

    static Quaternion changeAxis(Quaternion q)
    {
        return new Quaternion(-q.x, -q.y, q.z, q.w);
    }
}
