using UnityEngine;
using System.Collections;

public class DeviceDirection : MonoBehaviour {

    /// <summary>
    /// 使うセンサーの選択
    /// </summary>
    public enum ActiveSensor : int { GEOMAG = 0, GYRO, GYROGEOMAG }

    /// <summary>
    /// 方位計測に使うセンサー
    /// </summary>
    [SerializeField]
    ActiveSensor sensorMode = ActiveSensor.GEOMAG;

    /// <summary>
    /// 回転スピード
    /// </summary>
    [Range(1, 30), Tooltip("方位の変化する速度")]
    public float RotSpeed = 5;

    /// <summary>
    /// デバイスが動いていると判断する速度
    /// </summary>
    [Range(0, 10), Tooltip("ジャイロの感度閾値")]
    public float ThresRotRate = 5;

    /// <summary>
    /// デバイスの向いている方位
    /// </summary>
    Quaternion targetDirection;
    //最後にコンパスを更新した時刻
    double lastCompassTime;

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
    /// どのセンサーを使用しているか
    /// </summary>
    public ActiveSensor SensorMode { get { return sensorMode; } }
    
	// Use this for initialization
	void Start ()
    {
        //センサーの初期化
        SettingSensors(sensorMode);
    }

    // Update is called once per frame
    void Update ()
    {
        //デバイスの方位と傾き(ロール、ヨー、ピッチ角)を更新
        updateDeviceRot();

        //方位をスムーズに変異させる
        transform.rotation = Quaternion.Slerp(transform.rotation, targetDirection, Time.deltaTime * RotSpeed);
    }

    /// <summary>
    /// デバイスの方位を更新する
    /// </summary>
    void updateDeviceRot()
    {
        //加速度センサでロールとピッチ角をとるか
        bool accRollPith = true;
        //地磁気センサでヨー角をとるか
        bool geomagYaw = false;

        //ジャイロセンサのみ
        if (sensorMode == ActiveSensor.GYRO)
        {
            //デバイスの傾き(ピッチ、ロール角)は、デバイスの動く速さで決める
            accRollPith = Input.gyro.rotationRateUnbiased.magnitude < ThresRotRate;
        }
        //地磁気+ジャイロ
        else if (sensorMode == ActiveSensor.GYROGEOMAG)
        {
            //デバイスの傾き(ピッチ、ロール角)は、デバイスの動く速さで決める
            accRollPith = Input.gyro.rotationRateUnbiased.magnitude < ThresRotRate;
            geomagYaw = Input.compass.timestamp > lastCompassTime;
        }
        //デフォルト(地磁気のみ)
        else
        {
            geomagYaw = Input.compass.timestamp > lastCompassTime;
        }

        Vector3 gyroDir = GetGyroDir();
        //デバイスの傾き(ピッチ、ロール角)を求める
        if (accRollPith)
        {
            //加速度センサから
            GetAccGrad(out rolling, out pitching);
        }
        else
        {
            //ジャイロセンサから
            rolling = gyroDir.x;
            pitching = gyroDir.z;
        }
        //デバイスのヨー角を求める
        if(geomagYaw)
        {
            //時刻を更新
            lastCompassTime = Input.compass.timestamp;

            //オブジェクトの目標角度を変更する
            //ヨー角(Y軸回転)は地磁気から求める
            targetDirection = Quaternion.Euler(pitching, GetFlatNorth().y, rolling);
        }
        //ヨー角(Y軸回転)はジャイロから求める
        else if (Input.gyro.enabled)
        {
            //オブジェクトの目標角度を変更する
            targetDirection = Quaternion.Euler(pitching, gyroDir.y, rolling);
        }
    }

    /// <summary>
    /// 使うセンサーの初期化
    /// </summary>
    /// <param name="mode"></param>
    public void SettingSensors(ActiveSensor mode)
    {
        switch (mode)
        {
            //ジャイロセンサー
            case ActiveSensor.GYRO:
                //地磁気の無効化
                Input.compass.enabled = false;
                //ジャイロセンサをサポートしていたら、ジャイロを有効化
                Input.gyro.enabled = SystemInfo.supportsGyroscope;
                break;
            //地磁気+ジャイロ
            case ActiveSensor.GYROGEOMAG:
                //ジャイロセンサをサポートしていたら、ジャイロを有効化
                Input.gyro.enabled = SystemInfo.supportsGyroscope;
                //地磁気の有効化
                Input.compass.enabled = true;
                break;
            //デフォルト(地磁気センサー)
            case ActiveSensor.GEOMAG:
            default:
                //ジャイロセンサは無効化
                Input.gyro.enabled = false;
                //地磁気の有効化
                Input.compass.enabled = true;
                break;
        }

        //ジャイロが無効のままなら、地磁気モードへ変更
        if (!Input.gyro.enabled)
        {
            Input.compass.enabled = true;
            mode = ActiveSensor.GEOMAG;
        }

        //使うセンサーのモードを指定
        sensorMode = mode;
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
        Vector3 gravity = (Input.gyro.enabled) ? Input.gyro.gravity : Input.acceleration.normalized;
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
    /// ジャイロセンサによる方位を求める関数
    /// </summary>
    /// <returns>方位のオイラー角</returns>
    public static Vector3 GetGyroDir()
    {
        //ジャイロが搭載されていれば、ジャイロを有効
        Input.gyro.enabled = SystemInfo.supportsGyroscope;
        //無効化したままであれば、ゼロベクトルを返す
        if (Input.gyro.enabled) return Vector3.zero;

        //あらかじめデバイスの傾きを考慮したうえで、ジャイロの角度を返す
        Quaternion targetRotation = Quaternion.Euler(90, 0, 0) * changeAxis(Input.gyro.attitude);

        return targetRotation.eulerAngles;
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
    
    #region Utility
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
    #endregion
}
