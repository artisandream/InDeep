using UnityEngine;

namespace PlayWay.Water
{
	[ExecuteInEditMode]
	public class WaterDropsIME : MonoBehaviour
	{
		[SerializeField]
		private Shader waterDropsShader;

		[SerializeField]
		private Texture2D normalMap;

		private Material overlayMaterial;

		[SerializeField]
		private float intensity;
		public float Intensity
		{
			get { return intensity; }
			set
			{
				intensity = value;

				enabled = (value > 0.0f);
			}
		}

		void Awake()
		{
			if(overlayMaterial == null)
				CreateMaterial();

			if(intensity == 0.0f)
				enabled = false;
		}

		void OnValidate()
		{
			if(waterDropsShader == null)
				waterDropsShader = Shader.Find("PlayWay Water/IME/Water Drops");

			enabled = (intensity > 0.0f);
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if(overlayMaterial == null)
				CreateMaterial();

			overlayMaterial.SetFloat("_Intensity", intensity);

#if UNITY_EDITOR
			overlayMaterial.SetTexture("_NormalMap", normalMap);
#endif

			Graphics.Blit(source, destination, overlayMaterial, 0);

			if(intensity == 0.0f)
				enabled = false;
		}

		private void CreateMaterial()
		{
			overlayMaterial = new Material(waterDropsShader);
			overlayMaterial.hideFlags = HideFlags.DontSave;
			overlayMaterial.SetTexture("_NormalMap", normalMap);
		}
	}
}
