using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Displacement/Barrel Distortion")]
public class BarrelDistortion : PostEffectsBase {

    /// <summary>
    /// 自動補正をかけるか
    /// </summary>
    public bool Auto = false;

    /// <summary>
    /// FOVの半径設定
    /// </summary>
    [Range(0.1f, 2.0f)]
    public float FOV_Radians = 1.69f;

    /// <summary>
    /// 樽型補正シェーダー
    /// </summary>
    public Shader BarrelDistortionShader;

    /// <summary>
    /// 樽型補正マテリアル
    /// </summary>
    Material BarrelDistortionMaterial;
    
    /// <summary>
    /// リソースが用意されているかの確認
    /// </summary>
    /// <returns>リソースが不足していないか</returns>
    bool CheckResources()
    {
        CheckSupport(false);
        BarrelDistortionMaterial = CheckShaderAndCreateMaterial(BarrelDistortionShader, BarrelDistortionMaterial);

        if (!isSupported)
        {
            ReportAutoDisable();
        }

        return isSupported;
    }

    /// <summary>
    /// レンダリング画像のリアルタイムコピー
    /// </summary>
    /// <param name="src">元素材</param>
    /// <param name="dst">コピー先</param>
    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!CheckResources())
        {
            //元のテクスチャをシェーダーでレンダリングするテクスチャへコピー
            //主にimage effectで使用
            Graphics.Blit(src, dst);
            return;
        }

        if (Auto)
        {
            //FOVの自動計算
            FOV_Radians = GetComponent<Camera>().fieldOfView * Mathf.Deg2Rad;
        }

        //FOVの補正をかける
        BarrelDistortionMaterial.SetFloat("_FOV", FOV_Radians);

        //補正したマテリアルを適用
        Graphics.Blit(src, dst, BarrelDistortionMaterial);
    }
}
