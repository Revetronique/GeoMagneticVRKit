using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WebCamController : MonoBehaviour {
    
    /// <summary>
    /// 使用可能なデバイス一覧
    /// </summary>
    WebCamDevice[] devices;

    /// <summary>
    /// カメラ映像を出力するテクスチャ
    /// </summary>
    WebCamTexture webcamTexture;

    /// <summary>
    /// カメラ映像の出力先
    /// </summary>
    [SerializeField, Tooltip("出力先のカメラオブジェクト")]
    RawImage[] webcamOutput;

    /// <summary>
    /// 切り替え時間
    /// </summary>
    [SerializeField, Range(0.1f, 2.0f), Tooltip("画面の縦横切り替えの更新時間")]
    float timeCheckScreenDir = 0.3f;

    /// <summary>
    /// カメラ映像のテクスチャ
    /// </summary>
    public WebCamTexture DevCamTexture { get { return webcamTexture; } }

    /// <summary>
    /// カメラの解像度
    /// </summary>
    public Vector2 Resolution { get { return new Vector2(webcamTexture.width, webcamTexture.height); } }

    /// <summary>
    /// カメラが起動中かどうか
    /// </summary>
    public bool isPlaying { get { return (webcamTexture == null) ? false : webcamTexture.isPlaying; } }

    // Use this for initialization
    void Start ()
    {
        //カメラデバイスの取得
        devices = WebCamTexture.devices;
        
        //エディター時のみデバイス一覧を表示
#if UNITY_EDITOR
        //display all cameras
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log(devices[i].name);
        }
#endif
        
        //カメラの設定
        webcamTexture = new WebCamTexture(devices[0].name); //カメラのデフォルト設定
        
        //カメラの解像度変更
        setCameraRes(Screen.width, Screen.height);

        //出力先の決定
        foreach(RawImage output in webcamOutput)
        {
            output.texture = webcamTexture;

            if (Screen.orientation == ScreenOrientation.AutoRotation)
            {
                //デバイスの向き
                cameraFlipDeviceOrient(output.GetComponent<RectTransform>());
            }
            else
            {
                //画面の向きから映像を回転させる
                cameraFlip(output.GetComponent<RectTransform>());
            }
        }

        //スマートフォンの場合は、画面の傾きを見るコルーチンを起動
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
            //case RuntimePlatform.WP8Player:
                //0.1秒ごとに割り込む設定
                StartCoroutine(checkScreenRotation(timeCheckScreenDir));
                break;
        }
    }
    
    void OnDestroy()
    {
        //カメラの停止
        webcamTexture.Stop();
        webcamTexture = null;
    }

    /// <summary>
    /// 画面の向きからカメラ映像を適正な向きで表示
    /// </summary>
    void cameraFlip(RectTransform rectTransform)
    {
        //デバイスの向きがわからない場合は画面の向きを変えない
        if (Input.deviceOrientation == DeviceOrientation.Unknown) return;

        //画面の向きから映像を回転させる
        switch (Screen.orientation)
        {
            //横向き
            case ScreenOrientation.LandscapeRight:
                rectTransform.localEulerAngles = new Vector3(0, 0, 180);
                break;
            case ScreenOrientation.LandscapeLeft:
                rectTransform.localEulerAngles = new Vector3(0, 0, 0);
                break;
            //縦向き
            case ScreenOrientation.PortraitUpsideDown:
                rectTransform.localEulerAngles = new Vector3(0, 0, 90);
                break;
            case ScreenOrientation.Portrait:
                rectTransform.localEulerAngles = new Vector3(0, 0, 270);
                break;
        }
    }

    /// <summary>
    /// デバイスの向きからカメラ映像を適正な向きで表示
    /// </summary>
    void cameraFlipDeviceOrient(RectTransform rectTransform)
    {
        //画面の向きから映像を回転させる
        switch (Input.deviceOrientation)
        {
            //横向き
            case DeviceOrientation.LandscapeRight:
                rectTransform.localEulerAngles = new Vector3(0, 0, 180);
                break;
            case DeviceOrientation.LandscapeLeft:
                rectTransform.localEulerAngles = new Vector3(0, 0, 0);
                break;
            //縦向き
            case DeviceOrientation.PortraitUpsideDown:
                rectTransform.localEulerAngles = new Vector3(0, 0, 90);
                break;
            case DeviceOrientation.Portrait:
                rectTransform.localEulerAngles = new Vector3(0, 0, 270);
                break;
        }

        //カメラのテクスチャサイズを変更
        rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
    }

    /// <summary>
    /// 画面の向きからカメラの設定を調整するコルーチン
    /// </summary>
    /// <returns>待機時間</returns>
    IEnumerator checkScreenRotation(float waitTime)
    {
        while (true)
        {
            switch (Input.deviceOrientation)
            {
                case DeviceOrientation.Unknown:
                case DeviceOrientation.FaceUp:
                case DeviceOrientation.FaceDown:
                    yield return new WaitForSeconds(waitTime);
                    break;
                default:
                    if(Screen.orientation != (ScreenOrientation)Input.deviceOrientation)
                    {
                        //カメラの解像度変更
                        setCameraRes(Screen.width, Screen.height);
                        //画面の向き
                        foreach (RawImage output in webcamOutput)
                        {
                            cameraFlip(output.GetComponent<RectTransform>());   
                        }
                    }
                    yield return new WaitForSeconds(waitTime);
                    break;
            }
        }
    }

    /// <summary>
    /// カメラの解像度を変更する
    /// </summary>
    /// <param name="w">横</param>
    /// <param name="h">縦</param>
    void setCameraRes(int w, int h)
    {
        webcamTexture.Pause();
        webcamTexture.requestedWidth = w;
        webcamTexture.requestedHeight = h;
        webcamTexture.Play();
    }
    
    void getTakenPicture()
    {
        Color32[] color32 = webcamTexture.GetPixels32();

        Texture2D texture = new Texture2D(webcamTexture.width, webcamTexture.height);

        texture.SetPixels32(color32);
        texture.Apply();
    }

    /// <summary>
    /// 写真(テクスチャ)の画像化
    /// </summary>
    /// <param name="tex2d">元のテクスチャ</param>
    /// <param name="filename">ファイル名</param>
    /// <param name="filemode">ファイル形式(0:jpg, 1:png)</param>
    void savePicture(Texture2D tex2d, string filename, int filemode = 0)
    {
        byte[] bytes;

        switch (filemode)
        {
            case 0:
                bytes = tex2d.EncodeToJPG();
                System.IO.File.WriteAllBytes(Application.dataPath + filename + ".jpg", bytes);
                break;
            case 1:
                bytes = tex2d.EncodeToPNG();
                System.IO.File.WriteAllBytes(Application.dataPath + filename + ".png", bytes);
                break;
        }
    }

}
