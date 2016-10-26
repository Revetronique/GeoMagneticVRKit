using UnityEngine;
using UnityEngine.UI;

public class DebugPanel : MonoBehaviour {

    public GameObject panel;

    public Text indicatorOrientation;

    public Text indicatorRoll;
    public Text indicatorPitch;
    
    [SerializeField]
    bool debugMode = false;

    // Use this for initialization
    void Start () {
        panel.SetActive(debugMode);
	}
	
	// Update is called once per frame
	void Update () {
        //メニューボタンかDキーが押されたら
        if (Input.GetKeyUp(KeyCode.Menu) || Input.GetKeyUp(KeyCode.D))
        {
            //デバッグパネルを表示
            debugMode = !debugMode;
            panel.SetActive(debugMode);
        }

        if (debugMode)
        {
            indicatorOrientation.text = Camera.main.transform.rotation.eulerAngles.ToString();
            //indicatorOrientation.text = GameObject.Find("Canvas").GetComponent<Canvas>().pixelRect.ToString();

            float roll, pitch;
            DeviceDirection.GetAccGrad(out roll, out pitch);
            indicatorRoll.text = roll.ToString();
            indicatorPitch.text = pitch.ToString();
        }
	}
}
