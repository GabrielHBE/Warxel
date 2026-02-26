using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using static VolumetricClouds;

public class StateManager : MonoBehaviour
{
	public WeatherData weatherData;
	public string currentWeather;
	public float currentTime;
	private bool rain;
	private bool snow;
	private bool cloudy;
	private bool sunny;

	[SerializeField] private float weather_state_transition_speed = 1;

	[Header("Day/Night Cycle Configuration")]
	private float timeAccumulator = 43200f; // Começa às 12:00 (12 * 3600)
	[SerializeField] private DayNightCycle dayNightCycle = new DayNightCycle();

	[Header("Cloud Presets Configuration")]
	[SerializeField] private Volume volume;
	[SerializeField] private VolumetricClouds volumetricClouds;

	[Header("Skybox Settings")]
	[SerializeField] private Material skyboxMaterial;
	[SerializeField] private float skyboxExposure = 1f;

	[Header("Rain Stages Configuration")]
	[SerializeField] private RainStageConfiguration rainStageConfig = new RainStageConfiguration();

	[Header("Snow Stages Configuration")]
	[SerializeField] private SnowStageConfiguration snowStageConfig = new SnowStageConfiguration();

	[Header("Cloudy Stages Configuration")]
	[SerializeField] private CloudyStageConfiguration cloudyStageConfig = new CloudyStageConfiguration();

	[Header("Clear Stages Configuration")]
	[SerializeField] private ClearStageConfiguration clearStageConfig = new ClearStageConfiguration();

	[Header("Current Weather Stages")]
	[SerializeField] private RainIntensity currentRainIntensity = RainIntensity.None;
	[SerializeField] private SnowIntensity currentSnowIntensity = SnowIntensity.None;
	[SerializeField] private CloudyIntensity currentCloudyIntensity = CloudyIntensity.None;
	[SerializeField] private ClearIntensity currentClearIntensity = ClearIntensity.None;

	[Header("Lights")]
	[SerializeField] private Light sun_light;
	[SerializeField] private float sunLightMaxIntensity = 1f;

	[Header("Weather Objects")]
	[SerializeField] private Transform WeatherObjectsParent;
	[SerializeField] private GameObject rainObject;
	[SerializeField] private GameObject snowObject;
	[SerializeField] private GameObject cloudyObject;

	// Componentes cacheados
	private ParticleSystem rainParticleSystem;
	private AudioSource rainAudio;
	private ParticleSystem.EmissionModule rainEmissionModule;
	private ParticleSystem snowParticleSystem;
	private ParticleSystem windfParticleSystem;

	// Estado atual do dia/noite
	void Start()
	{
		currentTime = 12;

		if (!volume.profile.TryGet<VolumetricClouds>(out volumetricClouds))
		{
			volumetricClouds = volume.profile.Add<VolumetricClouds>();
		}

		// Obtém o material da skybox se não foi atribuído
		if (skyboxMaterial == null)
		{
			skyboxMaterial = RenderSettings.skybox;
		}

		// Salva a configuração padrão da skybox
		if (skyboxMaterial != null)
		{
			skyboxExposure = GetSkyboxExposure();
		}

		// Inicializa componentes da chuva
		if (rainObject != null)
		{
			rainParticleSystem = rainObject.GetComponent<ParticleSystem>();
			rainAudio = rainObject.GetComponent<AudioSource>();
			rainAudio.volume = 0;
			rainAudio.Play();
			if (rainParticleSystem != null)
			{
				rainEmissionModule = rainParticleSystem.emission;
			}
		}

		if (cloudyObject != null)
		{
			windfParticleSystem = cloudyObject.GetComponent<ParticleSystem>();
		}

		// Inicializa componentes da neve
		if (snowObject != null)
		{
			snowParticleSystem = snowObject.GetComponent<ParticleSystem>();
		}

		// Inicializa estágios
		currentRainIntensity = RainIntensity.None;
		currentSnowIntensity = SnowIntensity.None;
		currentCloudyIntensity = CloudyIntensity.None;
		currentClearIntensity = ClearIntensity.None;

		// Inicializa ciclo dia/noite
		dayNightCycle.Initialize(skyboxMaterial);

		// Aplica valores base do ciclo dia/noite imediatamente
		ApplyDayNightCycleBaseValues();
	}

	private string previousWeather = "";
	private bool wasClearWeather = false;

	void Update()
	{
		currentWeather = weatherData.Info.currently.icon;
		timeAccumulator += (Time.deltaTime * 3600f) / 20; // 1 hora real = 1 segundo no jogo
		currentTime = timeAccumulator;

		// Atualiza ciclo dia/noite
		dayNightCycle.UpdateTime(currentTime);

		UpdateSunRotation();




		// Aplica valores base do ciclo dia/noite (sempre aplicados)
		ApplyDayNightCycleBaseValues();



		// Verifica mudança de clima
		if (currentWeather != previousWeather)
		{
			wasClearWeather = previousWeather == "clear-day" || previousWeather == "clear-night";
			previousWeather = currentWeather;
		}

		if (currentWeather == "rain" || Input.GetKeyDown(KeyCode.F1))
		{
			SpawnRain();
		}
		else if (currentWeather == "snow" || currentWeather == "sleet" || Input.GetKeyDown(KeyCode.F2))
		{
			SpawnSnow();
		}
		else if (currentWeather == "cloudy" || currentWeather == "partly-cloudy-day" || currentWeather == "partly-cloudy-night" || currentWeather == "fog" || Input.GetKeyDown(KeyCode.F3))
		{
			SpawnCloudy();
		}
		else if (currentWeather == "clear-day" || currentWeather == "clear-night" || Input.GetKeyDown(KeyCode.F4))
		{
			SpawnClearWeather();
		}
	}

	#region Sun Rotation Control
	private void UpdateSunRotation()
	{
		if (sun_light == null) return;

		// Obtém a hora atual do ciclo dia/noite
		float currentHour = dayNightCycle.GetCurrentHour();

		// Calcula o ângulo baseado na hora:
		// 6h = 0°, 12h = 90°, 18h = 180°, 0h = 270°
		float sunAngle = CalculateSunAngle(currentHour);

		// Cria uma rotação apenas no eixo X
		// Mantém o azimute fixo (180° = sul, ou ajuste conforme necessário)
		Quaternion targetRotation = Quaternion.Euler(sunAngle, 180f, 0f);

		// Interpolação suave
		sun_light.transform.rotation = Quaternion.Slerp(sun_light.transform.rotation,
														targetRotation,
														Time.deltaTime * 0.5f);

	}

	private float CalculateSunAngle(float hour)
	{
		// Normaliza a hora para 0-24
		hour = Mathf.Repeat(hour, 24f);

		// Mapeia as horas para ângulos:
		if (hour >= 6f && hour < 18f)
		{
			// Dia: 6h (0°) -> 18h (180°)
			float t = (hour - 6f) / 12f;
			return Mathf.Lerp(0f, 180f, t);
		}
		else
		{
			// Noite: 18h (180°) -> 6h (360°/0°)
			float nightHour = hour < 6f ? hour + 24f : hour;
			float t = (nightHour - 18f) / 12f;
			return Mathf.Lerp(180f, 360f, t);
		}
	}

	#endregion

	#region Day/Night Cycle System - MODIFICADO
	[Serializable]
	public class DayNightCycle
	{
		private Material skyboxMaterial;

		[Header("Day/Night Time Configuration")]
		[SerializeField, Range(0, 23)] private int sunriseHour = 6;
		[SerializeField, Range(0, 23)] private int sunsetHour = 18;
		[SerializeField, Range(0, 23)] private int middayHour = 12;
		[SerializeField, Range(0, 23)] private int midnightHour = 0;

		[Header("Light Intensity Multipliers - BASE VALUES")]
		[SerializeField, Range(0, 2)] private float sunriseLightIntensity = 0.3f;
		[SerializeField, Range(0, 2)] private float middayLightIntensity = 1.0f;
		[SerializeField, Range(0, 2)] private float sunsetLightIntensity = 0.4f;
		[SerializeField, Range(0, 2)] private float nightLightIntensity = 0.1f;

		[Header("Skybox Intensity Multipliers - BASE VALUES")]
		[SerializeField, Range(0, 2)] private float sunriseSkyboxIntensity = 0.4f;
		[SerializeField, Range(0, 2)] private float middaySkyboxIntensity = 1.0f;
		[SerializeField, Range(0, 2)] private float sunsetSkyboxIntensity = 0.5f;
		[SerializeField, Range(0, 2)] private float nightSkyboxIntensity = 0.2f;

		[Header("Light Temperature - BASE VALUES")]
		[SerializeField, Range(1000, 20000)] private float sunriseTemperature = 3000f;
		[SerializeField, Range(1000, 20000)] private float middayTemperature = 6500f;
		[SerializeField, Range(1000, 20000)] private float sunsetTemperature = 2500f;
		[SerializeField, Range(1000, 20000)] private float nightTemperature = 8000f;


		private float currentHour = 12f;

		// Valores base atuais (SEM modificações de clima)
		private float baseLightIntensity = 1f;
		private float baseSkyboxIntensity = 1f;
		private float baseLightTemperature = 6500f;
		private Color baseSkyboxTint = Color.white;
		private Color baseAmbientLight = new Color(0.2f, 0.2f, 0.2f);

		public void Initialize(Material skyboxMaterial)
		{
			// Inicializa com meio-dia
			this.skyboxMaterial = skyboxMaterial;
			currentHour = middayHour;
			UpdateBaseValues();
		}

		public void UpdateTime(float unixTime)
		{
			// Normaliza o tempo para sempre ficar entre 0 e 86400 segundos (24 horas)
			float normalizedTime = unixTime % 86400f;

			// Converte para hora local (0-24)
			currentHour = normalizedTime / 3600f;

			// Ajusta para que em 12h o valor seja 0 (ponto de virada)
			float skyboxValue = Mathf.PingPong(currentHour / 12f + 1, 1f);
			// O +0.5f desloca a fase para que o pico ocorra em 6h e 18h
			skyboxMaterial.SetFloat("_DayTime", skyboxValue);
			UpdateBaseValues();
		}

		public float GetCurrentHour()
		{
			return currentHour;
		}

		private void UpdateBaseValues()
		{
			// Calcula valores BASE baseados na hora atual (sem modificações de clima)
			if (currentHour >= sunriseHour && currentHour < middayHour)
			{
				// Manhã: sunrise -> midday
				float t = (currentHour - sunriseHour) / (middayHour - sunriseHour);
				baseLightIntensity = Mathf.Lerp(sunriseLightIntensity, middayLightIntensity, t);
				baseSkyboxIntensity = Mathf.Lerp(sunriseSkyboxIntensity, middaySkyboxIntensity, t);
				baseLightTemperature = Mathf.Lerp(sunriseTemperature, middayTemperature, t);
			}
			else if (currentHour >= middayHour && currentHour < sunsetHour)
			{
				// Tarde: midday -> sunset
				float t = (currentHour - middayHour) / (sunsetHour - middayHour);
				baseLightIntensity = Mathf.Lerp(middayLightIntensity, sunsetLightIntensity, t);
				baseSkyboxIntensity = Mathf.Lerp(middaySkyboxIntensity, sunsetSkyboxIntensity, t);
				baseLightTemperature = Mathf.Lerp(middayTemperature, sunsetTemperature, t);
			}
			else if (currentHour >= sunsetHour || currentHour < sunriseHour)
			{
				// Noite: sunset -> sunrise
				float hour = currentHour < sunriseHour ? currentHour + 24 : currentHour;
				float t = (hour - sunsetHour) / (24 - sunsetHour + sunriseHour);
				baseLightIntensity = Mathf.Lerp(sunsetLightIntensity, nightLightIntensity, t);
				baseSkyboxIntensity = Mathf.Lerp(sunsetSkyboxIntensity, nightSkyboxIntensity, t);
				baseLightTemperature = Mathf.Lerp(sunsetTemperature, nightTemperature, t);
			}
		}

		// Métodos para obter valores BASE (para serem modificados pelo clima)
		public float GetBaseLightIntensity()
		{
			return baseLightIntensity;
		}

		public float GetBaseSkyboxIntensity()
		{
			return baseSkyboxIntensity;
		}

		public float GetBaseLightTemperature()
		{
			return baseLightTemperature;
		}

		public Color GetBaseAmbientLight()
		{
			return baseAmbientLight;
		}

		public bool IsNightTime()
		{
			return currentHour >= sunsetHour || currentHour < sunriseHour;
		}

		public bool IsDayTime()
		{
			return !IsNightTime();
		}
	}
	#endregion

	#region Aplicação dos valores base do ciclo dia/noite
	private void ApplyDayNightCycleBaseValues()
	{
		// Obtém valores BASE do ciclo dia/noite
		float baseLightIntensity = dayNightCycle.GetBaseLightIntensity();
		float baseSkyboxIntensity = dayNightCycle.GetBaseSkyboxIntensity();
		float baseLightTemperature = dayNightCycle.GetBaseLightTemperature();

		// Aplica valores BASE (serão modificados pelo clima se houver um ativo)
		// 1. Aplica intensidade da luz do sol
		if (sun_light != null)
		{
			sun_light.intensity = baseLightIntensity * sunLightMaxIntensity;

			// Aplica temperatura da luz
			if (sun_light.useColorTemperature)
			{
				sun_light.colorTemperature = baseLightTemperature;
			}
		}

		// 2. Aplica intensidade da skybox
		RenderSettings.ambientIntensity = baseSkyboxIntensity;

	}
	#endregion

	#region Rain Intensity Control - MODIFICADO
	public enum RainIntensity
	{
		None,
		Light,
		Medium,
		Heavy
	}

	[Serializable]
	public class RainStage
	{
		public string stageName;
		[Range(0, 0.04f)] public float fogIntensity;
		[Range(100, 40000)] public float rainRate;

		// MODIFICADO: Agora são MODIFICADORES em relação aos valores base
		[Range(-1, 1)] public float lightIntensityModifier; // -1 a 1: redução ou aumento em relação ao base
		[Range(-1, 1)] public float skyboxIntensityModifier; // -1 a 1: redução ou aumento em relação ao base
		[Range(-5000, 5000)] public float lightTemperatureModifier; // -5000 a +5000: ajuste em relação ao base


		public float skyboxExposure = 1f;
		public Color fogColor = Color.gray;

		public RainStage(string name, float fog, float rainRt,
						float lightIntMod, float skyboxIntMod, float lightTempMod,
						float exposure, Color fogCol)
		{
			stageName = name;
			fogIntensity = fog;
			rainRate = rainRt;
			lightIntensityModifier = lightIntMod;
			skyboxIntensityModifier = skyboxIntMod;
			lightTemperatureModifier = lightTempMod;
			skyboxExposure = exposure;
			fogColor = fogCol;
		}
	}

	[Serializable]
	public class RainStageConfig
	{
		public RainStage stage;
		public CloudPresetSettings cloudPreset;

		public RainStageConfig(RainStage stage, CloudPresetSettings cloudPreset)
		{
			this.stage = stage;
			this.cloudPreset = cloudPreset;
		}
	}

	[Serializable]
	public class RainStageConfiguration
	{
		[Header("Light Rain Configuration")]
		public RainStageConfig lightRainStage = new RainStageConfig(
			new RainStage("Light Rain", 0.3f, 0.3f,
				-0.2f,    // Reduz 20% da intensidade da luz base
				-0.3f,    // Reduz 30% da intensidade da skybox base
				-2000f,   // Reduz 2000K da temperatura base
				0.8f,                         // Skybox exposure
				new Color(0.5f, 0.5f, 0.6f)), // Fog color
			new CloudPresetSettings("Light Rain Clouds")
			{
				densityMultiplier = 0.6f,
				shapeFactor = 0.8f,
				shapeScale = 5.0f,
				erosionFactor = 0.8f,
				erosionScale = 107.0f,
				bottomAltitude = 1200.0f,
				altitudeRange = 2000.0f,
				sunLightDimmer = 0.7f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);

		[Header("Medium Rain Configuration")]
		public RainStageConfig mediumRainStage = new RainStageConfig(
			new RainStage("Medium Rain", 0.6f, 0.6f,
				-0.4f,    // Reduz 40% da intensidade da luz base
				-0.6f,    // Reduz 60% da intensidade da skybox base
				-3000f,   // Reduz 3000K da temperatura base
				0.6f,                         // Skybox exposure
				new Color(0.4f, 0.4f, 0.5f)), // Fog color
			new CloudPresetSettings("Stormy Clouds")
			{
				densityMultiplier = 0.9f,
				shapeFactor = 0.85f,
				shapeScale = 6.0f,
				erosionFactor = 0.75f,
				erosionScale = 120.0f,
				bottomAltitude = 1000.0f,
				altitudeRange = 5000.0f,
				globalSpeed = 50.0f,
				sunLightDimmer = 0.3f,
				ambientLightProbeDimmer = 0.8f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);

		[Header("Heavy Rain Configuration")]
		public RainStageConfig heavyRainStage = new RainStageConfig(
			new RainStage("Heavy Rain", 1.0f, 1.0f,
				-0.6f,    // Reduz 60% da intensidade da luz base
				-0.9f,    // Reduz 90% da intensidade da skybox base
				-3500f,   // Reduz 3500K da temperatura base
				0.4f,                         // Skybox exposure
				new Color(0.3f, 0.3f, 0.4f)), // Fog color
			new CloudPresetSettings("Heavy Rain Clouds")
			{
				densityMultiplier = 1.0f,
				shapeFactor = 0.8f,
				shapeScale = 7.0f,
				erosionFactor = 0.9f,
				erosionScale = 150.0f,
				bottomAltitude = 800.0f,
				altitudeRange = 6000.0f,
				globalSpeed = 80.0f,
				sunLightDimmer = 0.2f,
				ambientLightProbeDimmer = 0.6f,
				useMicroErosion = true,
				microErosionFactor = 0.6f,
				microErosionScale = 200.0f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
			}
		);
	}
	#endregion

	#region Snow Intensity Control - MODIFICADO
	public enum SnowIntensity
	{
		None,
		Light,
		Medium,
		Heavy
	}

	[Serializable]
	public class SnowStage
	{
		public string stageName;
		[Range(0, 0.04f)] public float fogIntensity;
		[Range(100, 40000)] public float snowRate;

		// MODIFICADO: Agora são MODIFICADORES
		[Range(-1, 1)] public float lightIntensityModifier;
		[Range(-1, 1)] public float skyboxIntensityModifier;
		[Range(-5000, 5000)] public float lightTemperatureModifier;

		public float skyboxExposure = 1f;
		public Color fogColor = Color.gray;

		public SnowStage(string name, float fog, float snowRt,
						float lightIntMod, float skyboxIntMod, float lightTempMod,
						float exposure, Color fogCol)
		{
			stageName = name;
			fogIntensity = fog;
			snowRate = snowRt;
			lightIntensityModifier = lightIntMod;
			skyboxIntensityModifier = skyboxIntMod;
			lightTemperatureModifier = lightTempMod;
			skyboxExposure = exposure;
			fogColor = fogCol;
		}
	}

	[Serializable]
	public class SnowStageConfig
	{
		public SnowStage stage;
		public CloudPresetSettings cloudPreset;

		public SnowStageConfig(SnowStage stage, CloudPresetSettings cloudPreset)
		{
			this.stage = stage;
			this.cloudPreset = cloudPreset;
		}
	}

	[Serializable]
	public class SnowStageConfiguration
	{
		[Header("Light Snow Configuration")]
		public SnowStageConfig lightSnowStage = new SnowStageConfig(
			new SnowStage("Light Snow", 0.2f, 0.3f,
				-0.1f,    // Reduz 10% da intensidade da luz base
				-0.2f,    // Reduz 20% da intensidade da skybox base
				-1000f,   // Reduz 1000K da temperatura base
				0.9f,                          // Skybox exposure
				new Color(0.7f, 0.75f, 0.85f)), // Fog color
			new CloudPresetSettings("Light Snow Clouds")
			{
				densityMultiplier = 0.5f,
				shapeFactor = 0.9f,
				shapeScale = 6.0f,
				erosionFactor = 0.85f,
				erosionScale = 110.0f,
				bottomAltitude = 1500.0f,
				altitudeRange = 2500.0f,
				sunLightDimmer = 0.8f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);

		[Header("Medium Snow Configuration")]
		public SnowStageConfig mediumSnowStage = new SnowStageConfig(
			new SnowStage("Medium Snow", 0.4f, 0.6f,
				-0.3f,    // Reduz 30% da intensidade da luz base
				-0.4f,    // Reduz 40% da intensidade da skybox base
				-1500f,   // Reduz 1500K da temperatura base
				0.7f,                          // Skybox exposure
				new Color(0.6f, 0.65f, 0.75f)), // Fog color
			new CloudPresetSettings("Medium Snow Clouds")
			{
				densityMultiplier = 0.8f,
				shapeFactor = 0.85f,
				shapeScale = 7.0f,
				erosionFactor = 0.8f,
				erosionScale = 130.0f,
				bottomAltitude = 1300.0f,
				altitudeRange = 20000.0f,
				sunLightDimmer = 0.5f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);

		[Header("Heavy Snow Configuration")]
		public SnowStageConfig heavySnowStage = new SnowStageConfig(
			new SnowStage("Heavy Snow", 0.7f, 1.0f,
				-0.5f,    // Reduz 50% da intensidade da luz base
				-0.7f,    // Reduz 70% da intensidade da skybox base
				-2000f,   // Reduz 2000K da temperatura base
				0.5f,                          // Skybox exposure
				new Color(0.5f, 0.55f, 0.65f)), // Fog color
			new CloudPresetSettings("Heavy Snow Clouds")
			{
				densityMultiplier = 1.0f,
				shapeFactor = 0.8f,
				shapeScale = 8.0f,
				erosionFactor = 0.9f,
				erosionScale = 160.0f,
				bottomAltitude = 1000.0f,
				altitudeRange = 5000.0f,
				sunLightDimmer = 0.3f,
				ambientLightProbeDimmer = 0.7f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);
	}
	#endregion

	#region Cloudy Intensity Control - MODIFICADO
	public enum CloudyIntensity
	{
		None,
		PartlyCloudy,
		Cloudy,
		Overcast
	}

	[Serializable]
	public class CloudyStage
	{
		public string stageName;
		[Range(0, 2000f)] public float wind_speed;
		[Range(0, 0.04f)] public float fogIntensity;

		// MODIFICADO: Agora são MODIFICADORES
		[Range(-1, 1)] public float lightIntensityModifier;
		[Range(-1, 1)] public float skyboxIntensityModifier;
		[Range(-5000, 5000)] public float lightTemperatureModifier;

		public float skyboxExposure = 1f;
		public Color fogColor = Color.gray;

		public CloudyStage(string name, float fog,
						float lightIntMod, float skyboxIntMod, float lightTempMod,
						float exposure, Color fogCol, float ws)
		{
			stageName = name;
			fogIntensity = fog;
			lightIntensityModifier = lightIntMod;
			skyboxIntensityModifier = skyboxIntMod;
			lightTemperatureModifier = lightTempMod;
			skyboxExposure = exposure;
			fogColor = fogCol;
			wind_speed = ws;
		}
	}

	[Serializable]
	public class CloudyStageConfig
	{
		public CloudyStage stage;
		public CloudPresetSettings cloudPreset;

		public CloudyStageConfig(CloudyStage stage, CloudPresetSettings cloudPreset)
		{
			this.stage = stage;
			this.cloudPreset = cloudPreset;
		}
	}

	[Serializable]
	public class CloudyStageConfiguration
	{
		[Header("Partly Cloudy Configuration")]
		public CloudyStageConfig partlyCloudyStage = new CloudyStageConfig(
			new CloudyStage("Partly Cloudy", 0.1f,
				-0.05f,   // Reduz 5% da intensidade da luz base
				-0.1f,    // Reduz 10% da intensidade da skybox base
				-500f,    // Reduz 500K da temperatura base
				0.95f,                          // Skybox exposure
				new Color(0.8f, 0.8f, 0.85f),
				400),  // Fog color

			new CloudPresetSettings("Partly Cloudy")
			{
				densityMultiplier = 0.3f,
				shapeFactor = 0.95f,
				shapeScale = 4.0f,
				erosionFactor = 0.9f,
				erosionScale = 100.0f,
				bottomAltitude = 2000.0f,
				altitudeRange = 1500.0f,
				sunLightDimmer = 0.9f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);

		[Header("Cloudy Configuration")]
		public CloudyStageConfig cloudyStage = new CloudyStageConfig(
			new CloudyStage("Cloudy", 0.2f,
				-0.2f,    // Reduz 20% da intensidade da luz base
				-0.3f,    // Reduz 30% da intensidade da skybox base
				-1000f,   // Reduz 1000K da temperatura base
				0.8f,                          // Skybox exposure
				new Color(0.7f, 0.7f, 0.75f),
				1200),  // Fog color
			new CloudPresetSettings("Cloudy")
			{
				densityMultiplier = 0.7f,
				shapeFactor = 0.9f,
				shapeScale = 6.0f,
				erosionFactor = 0.85f,
				erosionScale = 115.0f,
				bottomAltitude = 1500.0f,
				altitudeRange = 3000.0f,
				sunLightDimmer = 0.6f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);

		[Header("Overcast Configuration")]
		public CloudyStageConfig overcastStage = new CloudyStageConfig(
			new CloudyStage("Overcast", 0.3f,
				-0.4f,    // Reduz 40% da intensidade da luz base
				-0.6f,    // Reduz 60% da intensidade da skybox base
				-1500f,   // Reduz 1500K da temperatura base
				0.6f,                           // Skybox exposure
				new Color(0.6f, 0.6f, 0.65f),
				2000),   // Fog color

			new CloudPresetSettings("Overcast")
			{
				densityMultiplier = 1.0f,
				shapeFactor = 0.85f,
				shapeScale = 7.0f,
				erosionFactor = 0.8f,
				erosionScale = 140.0f,
				bottomAltitude = 1200.0f,
				altitudeRange = 40000.0f,
				sunLightDimmer = 0.4f,
				ambientLightProbeDimmer = 0.8f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);
	}
	#endregion

	#region Clear Intensity Control - MODIFICADO
	public enum ClearIntensity
	{
		None,
		ClearDay,
		ClearNight
	}

	[Serializable]
	public class ClearStage
	{
		public string stageName;
		[Range(0, 0.04f)] public float fogIntensity;

		// MODIFICADO: Agora são MODIFICADORES
		[Range(-1, 1)] public float lightIntensityModifier;
		[Range(-1, 1)] public float skyboxIntensityModifier;
		[Range(-5000, 5000)] public float lightTemperatureModifier;

		public float skyboxExposure = 1f;
		public Color fogColor = Color.gray;

		public ClearStage(string name, float fog,
						float lightIntMod, float skyboxIntMod, float lightTempMod,
						float exposure, Color fogCol)
		{
			stageName = name;
			fogIntensity = fog;
			lightIntensityModifier = lightIntMod;
			skyboxIntensityModifier = skyboxIntMod;
			lightTemperatureModifier = lightTempMod;
			skyboxExposure = exposure;
			fogColor = fogCol;
		}
	}

	[Serializable]
	public class ClearStageConfig
	{
		public ClearStage stage;
		public CloudPresetSettings cloudPreset;

		public ClearStageConfig(ClearStage stage, CloudPresetSettings cloudPreset)
		{
			this.stage = stage;
			this.cloudPreset = cloudPreset;
		}
	}

	[Serializable]
	public class ClearStageConfiguration
	{
		[Header("Clear Day Configuration")]
		public ClearStageConfig clearDayStage = new ClearStageConfig(
			new ClearStage("Clear Day", 0f,
				0f,       // Sem modificação na intensidade da luz base
				0f,       // Sem modificação na intensidade da skybox base
				0f,       // Sem modificação na temperatura base
				1.0f,                          // Skybox exposure
				new Color(0.8f, 0.85f, 1.0f)),  // Fog color
			new CloudPresetSettings("Clear Day")
			{
				densityMultiplier = 0.1f,
				shapeFactor = 1.0f,
				shapeScale = 3.0f,
				erosionFactor = 0.95f,
				erosionScale = 90.0f,
				bottomAltitude = 2500.0f,
				altitudeRange = 1000.0f,
				sunLightDimmer = 1.0f,
				ambientLightProbeDimmer = 1.0f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);

		[Header("Clear Night Configuration")]
		public ClearStageConfig clearNightStage = new ClearStageConfig(
			new ClearStage("Clear Night", 0.1f,
				0f,       // Sem modificação na intensidade da luz base (já é baixa à noite)
				0f,       // Sem modificação na intensidade da skybox base (já é baixa à noite)
				0f,       // Sem modificação na temperatura base
				0.3f,                          // Skybox exposure
				new Color(0.05f, 0.05f, 0.1f)), // Fog color
			new CloudPresetSettings("Clear Night")
			{
				densityMultiplier = 0.2f,
				shapeFactor = 0.98f,
				shapeScale = 4.0f,
				erosionFactor = 0.92f,
				erosionScale = 95.0f,
				bottomAltitude = 2200.0f,
				altitudeRange = 1200.0f,
				sunLightDimmer = 0.2f,
				ambientLightProbeDimmer = 0.5f,

				// Valores padrão para as novas propriedades
				shapeOffset = Vector3.zero,
				shapeSpeedMultiplier = 1.0f,
				erosionSpeedMultiplier = 0.25f,
				altitudeDistortion = 0.25f,
				verticalShapeWindSpeed = 0.0f,
				verticalErosionWindSpeed = 0.0f,
				erosionOcclusion = 0.1f,
				scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f),
				powderEffectIntensity = 0.25f,
				multiScattering = 0.5f,
				shadows = false,
				shadowResolution = CloudShadowResolution.Medium256,
				shadowDistance = 8000.0f,
				shadowOpacity = 1.0f,
				shadowOpacityFallback = 0.0f,
				temporalAccumulationFactor = 0.95f,
				perceptualBlending = 1.0f,
				numPrimarySteps = 32,
				numLightSteps = 2,
				fadeInMode = CloudFadeInMode.Automatic,
				fadeInStart = 0.0f,
				fadeInDistance = 5000.0f,
				localClouds = true,
				useMicroErosion = false
			}
		);
	}
	#endregion



	#region Common Methods
	private RainStageConfig GetCurrentRainStageConfig()
	{
		switch (currentRainIntensity)
		{
			case RainIntensity.Light:
				return rainStageConfig.lightRainStage;
			case RainIntensity.Medium:
				return rainStageConfig.mediumRainStage;
			case RainIntensity.Heavy:
				return rainStageConfig.heavyRainStage;
			default:
				return rainStageConfig.lightRainStage;
		}
	}

	private SnowStageConfig GetCurrentSnowStageConfig()
	{
		switch (currentSnowIntensity)
		{
			case SnowIntensity.Light:
				return snowStageConfig.lightSnowStage;
			case SnowIntensity.Medium:
				return snowStageConfig.mediumSnowStage;
			case SnowIntensity.Heavy:
				return snowStageConfig.heavySnowStage;
			default:
				return snowStageConfig.lightSnowStage;
		}
	}

	private CloudyStageConfig GetCurrentCloudyStageConfig()
	{
		switch (currentCloudyIntensity)
		{
			case CloudyIntensity.PartlyCloudy:
				return cloudyStageConfig.partlyCloudyStage;
			case CloudyIntensity.Cloudy:
				return cloudyStageConfig.cloudyStage;
			case CloudyIntensity.Overcast:
				return cloudyStageConfig.overcastStage;
			default:
				return cloudyStageConfig.partlyCloudyStage;
		}
	}

	private ClearStageConfig GetCurrentClearStageConfig()
	{
		if (currentWeather == "clear-night")
		{
			return clearStageConfig.clearNightStage;
		}
		return clearStageConfig.clearDayStage;
	}

	// Método para aplicar modificadores do clima sobre os valores base
	private void ApplyWeatherModifiers(float lightIntensityMod, float skyboxIntensityMod, float lightTempMod,
								  float fogIntensity,
								  float skyboxExposure, Color fogColor)
	{
		// Obtém valores BASE do ciclo dia/noite
		float baseLightIntensity = dayNightCycle.GetBaseLightIntensity();
		float baseSkyboxIntensity = dayNightCycle.GetBaseSkyboxIntensity();
		float baseLightTemperature = dayNightCycle.GetBaseLightTemperature();


		// Aplica modificadores ADITIVOS sobre os valores base
		// 1. Intensidade da luz: base + modificador (limitado entre 0 e 1)
		float finalLightIntensity = Mathf.Clamp01(baseLightIntensity + lightIntensityMod) * sunLightMaxIntensity;

		// 2. Intensidade da skybox: base + modificador (limitado entre 0 e 1)
		float finalSkyboxIntensity = Mathf.Clamp01(baseSkyboxIntensity + skyboxIntensityMod);

		// 3. Temperatura da luz: base + modificador (limitado entre 1000 e 20000)
		float finalLightTemperature = Mathf.Clamp(baseLightTemperature + lightTempMod, 1000f, 20000f);


		// 5. Aplica fog intensity e cor (não afetada pelo ciclo)
		if (RenderSettings.fog)
		{
			RenderSettings.fogDensity = fogIntensity;
			RenderSettings.fogColor = fogColor;
		}

		// 7. Aplica valores finais
		if (sun_light != null)
		{
			sun_light.intensity = finalLightIntensity;

			if (sun_light.useColorTemperature)
			{
				sun_light.colorTemperature = finalLightTemperature;
			}
		}

		// 8. Aplica intensidade da skybox
		RenderSettings.ambientIntensity = finalSkyboxIntensity;

	}

	private RainStage GetCurrentRainParameters()
	{
		float currentFog = RenderSettings.fog ? RenderSettings.fogDensity : 0f;
		float currentRainRate = rainEmissionModule.rateOverTimeMultiplier;
		float currentLightIntensity = sun_light != null ? sun_light.intensity / sunLightMaxIntensity : 1f;
		float currentTemperature = sun_light != null && sun_light.useColorTemperature ?
			sun_light.colorTemperature : 6500f;
		float currentSkyboxExposure = GetSkyboxExposure();
		Color currentFogColor = RenderSettings.fog ? RenderSettings.fogColor : Color.gray;

		// Calcula modificadores atuais em relação aos valores base
		float baseLightIntensity = dayNightCycle.GetBaseLightIntensity();
		float baseSkyboxIntensity = dayNightCycle.GetBaseSkyboxIntensity();
		float baseLightTemperature = dayNightCycle.GetBaseLightTemperature();

		float lightIntensityMod = currentLightIntensity - baseLightIntensity;
		float skyboxIntensityMod = (RenderSettings.ambientIntensity) - baseSkyboxIntensity;
		float lightTemperatureMod = currentTemperature - baseLightTemperature;

		return new RainStage("Current",
			currentFog,
			currentRainRate,
			lightIntensityMod,
			skyboxIntensityMod,
			lightTemperatureMod,
			currentSkyboxExposure,
			currentFogColor);
	}

	private SnowStage GetCurrentSnowParameters()
	{
		float currentFog = RenderSettings.fog ? RenderSettings.fogDensity : 0f;
		float currentSnowRate = snowParticleSystem != null ? snowParticleSystem.emission.rateOverTimeMultiplier : 0f;
		float currentLightIntensity = sun_light != null ? sun_light.intensity / sunLightMaxIntensity : 1f;
		float currentTemperature = sun_light != null && sun_light.useColorTemperature ?
			sun_light.colorTemperature : 6500f;
		float currentSkyboxExposure = GetSkyboxExposure();
		Color currentFogColor = RenderSettings.fog ? RenderSettings.fogColor : Color.gray;

		return new SnowStage("Current",
			currentFog,
			currentSnowRate,
			1,
			currentLightIntensity,
			currentTemperature,
			currentSkyboxExposure,
			currentFogColor);
	}

	private CloudyStage GetCurrentCloudyParameters()
	{
		float currentFog = RenderSettings.fog ? RenderSettings.fogDensity : 0f;
		float currentLightIntensity = sun_light != null ? sun_light.intensity / sunLightMaxIntensity : 1f;
		float currentTemperature = sun_light != null && sun_light.useColorTemperature ?
			sun_light.colorTemperature : 6500f;
		float current_wind_speed = windfParticleSystem.emission.rateOverTimeMultiplier;
		float currentSkyboxExposure = GetSkyboxExposure();
		Color currentAmbientLight = RenderSettings.ambientLight;
		Color currentFogColor = RenderSettings.fog ? RenderSettings.fogColor : Color.gray;

		// Calcula modificadores atuais
		float baseLightIntensity = dayNightCycle.GetBaseLightIntensity();
		float baseSkyboxIntensity = dayNightCycle.GetBaseSkyboxIntensity();
		float baseLightTemperature = dayNightCycle.GetBaseLightTemperature();

		float lightIntensityMod = currentLightIntensity - baseLightIntensity;
		float skyboxIntensityMod = (RenderSettings.ambientIntensity) - baseSkyboxIntensity;
		float lightTemperatureMod = currentTemperature - baseLightTemperature;


		return new CloudyStage("Current",
			currentFog,
			lightIntensityMod,
			skyboxIntensityMod,
			lightTemperatureMod,
			currentSkyboxExposure,
			currentFogColor,
			current_wind_speed);
	}

	private ClearStage GetCurrentClearParameters()
	{
		float currentFog = RenderSettings.fog ? RenderSettings.fogDensity : 0f;
		float currentLightIntensity = sun_light != null ? sun_light.intensity / sunLightMaxIntensity : 1f;
		float currentTemperature = sun_light != null && sun_light.useColorTemperature ?
			sun_light.colorTemperature : 6500f;
		float currentSkyboxExposure = GetSkyboxExposure();
		Color currentFogColor = RenderSettings.fog ? RenderSettings.fogColor : Color.gray;

		// Calcula modificadores atuais
		float baseLightIntensity = dayNightCycle.GetBaseLightIntensity();
		float baseSkyboxIntensity = dayNightCycle.GetBaseSkyboxIntensity();
		float baseLightTemperature = dayNightCycle.GetBaseLightTemperature();


		float lightIntensityMod = currentLightIntensity - baseLightIntensity;
		float skyboxIntensityMod = (RenderSettings.ambientIntensity) - baseSkyboxIntensity;
		float lightTemperatureMod = currentTemperature - baseLightTemperature;

		return new ClearStage("Current",
			currentFog,
			lightIntensityMod,
			skyboxIntensityMod,
			lightTemperatureMod,
			currentSkyboxExposure,
			currentFogColor);
	}

	// Método auxiliar para dividir cores (usado para calcular modificadores)
	private Color DivideColors(Color a, Color b)
	{
		return new Color(
			b.r > 0 ? a.r / b.r : 1f,
			b.g > 0 ? a.g / b.g : 1f,
			b.b > 0 ? a.b / b.b : 1f
		);
	}

	private CloudPresetSettings GetCurrentCloudPreset()
	{
		return new CloudPresetSettings("CurrentClouds")
		{
			densityMultiplier = volumetricClouds.densityMultiplier.value,
			shapeFactor = volumetricClouds.shapeFactor.value,
			shapeScale = volumetricClouds.shapeScale.value,
			erosionFactor = volumetricClouds.erosionFactor.value,
			erosionScale = volumetricClouds.erosionScale.value,
			bottomAltitude = volumetricClouds.bottomAltitude.value,
			altitudeRange = volumetricClouds.altitudeRange.value,
			microErosionFactor = volumetricClouds.microErosionFactor.value,
			microErosionScale = volumetricClouds.microErosionScale.value,
			globalSpeed = volumetricClouds.globalSpeed.value,
			globalOrientation = volumetricClouds.globalOrientation.value,
			ambientLightProbeDimmer = volumetricClouds.ambientLightProbeDimmer.value,
			sunLightDimmer = volumetricClouds.sunLightDimmer.value,

			// NOVAS PROPRIEDADES
			shapeOffset = volumetricClouds.shapeOffset.value,
			shapeSpeedMultiplier = volumetricClouds.shapeSpeedMultiplier.value,
			erosionSpeedMultiplier = volumetricClouds.erosionSpeedMultiplier.value,
			altitudeDistortion = volumetricClouds.altitudeDistortion.value,
			verticalShapeWindSpeed = volumetricClouds.verticalShapeWindSpeed.value,
			verticalErosionWindSpeed = volumetricClouds.verticalErosionWindSpeed.value,
			erosionOcclusion = volumetricClouds.erosionOcclusion.value,
			scatteringTint = volumetricClouds.scatteringTint.value,
			powderEffectIntensity = volumetricClouds.powderEffectIntensity.value,
			multiScattering = volumetricClouds.multiScattering.value,
			shadows = volumetricClouds.shadows.value,
			shadowResolution = volumetricClouds.shadowResolution.value,
			shadowDistance = volumetricClouds.shadowDistance.value,
			shadowOpacity = volumetricClouds.shadowOpacity.value,
			shadowOpacityFallback = volumetricClouds.shadowOpacityFallback.value,
			temporalAccumulationFactor = volumetricClouds.temporalAccumulationFactor.value,
			perceptualBlending = volumetricClouds.perceptualBlending.value,
			numPrimarySteps = volumetricClouds.numPrimarySteps.value,
			numLightSteps = volumetricClouds.numLightSteps.value,
			fadeInMode = volumetricClouds.fadeInMode.value,
			fadeInStart = volumetricClouds.fadeInStart.value,
			fadeInDistance = volumetricClouds.fadeInDistance.value,
			localClouds = volumetricClouds.localClouds.value,

			// Curves
			densityCurve = volumetricClouds.densityCurve.value,
			erosionCurve = volumetricClouds.erosionCurve.value,
			ambientOcclusionCurve = volumetricClouds.ambientOcclusionCurve.value
		};
	}

	private void TransitionCloudPreset(CloudPresetSettings startPreset, CloudPresetSettings targetPreset, float t)
	{
		volumetricClouds.cloudPreset = VolumetricClouds.CloudPresets.Custom;

		// Propriedades existentes
		volumetricClouds.densityMultiplier.value = Mathf.Lerp(
			startPreset.densityMultiplier, targetPreset.densityMultiplier, t);
		volumetricClouds.shapeFactor.value = Mathf.Lerp(
			startPreset.shapeFactor, targetPreset.shapeFactor, t);
		volumetricClouds.shapeScale.value = Mathf.Lerp(
			startPreset.shapeScale, targetPreset.shapeScale, t);
		volumetricClouds.erosionFactor.value = Mathf.Lerp(
			startPreset.erosionFactor, targetPreset.erosionFactor, t);
		volumetricClouds.erosionScale.value = Mathf.Lerp(
			startPreset.erosionScale, targetPreset.erosionScale, t);
		volumetricClouds.bottomAltitude.value = Mathf.Lerp(
			startPreset.bottomAltitude, targetPreset.bottomAltitude, t);
		volumetricClouds.altitudeRange.value = Mathf.Lerp(
			startPreset.altitudeRange, targetPreset.altitudeRange, t);
		volumetricClouds.microErosionFactor.value = Mathf.Lerp(
			startPreset.microErosionFactor, targetPreset.microErosionFactor, t);
		volumetricClouds.microErosionScale.value = Mathf.Lerp(
			startPreset.microErosionScale, targetPreset.microErosionScale, t);
		volumetricClouds.globalSpeed.value = Mathf.Lerp(
			startPreset.globalSpeed, targetPreset.globalSpeed, t);
		volumetricClouds.globalOrientation.value = Mathf.Lerp(
			startPreset.globalOrientation, targetPreset.globalOrientation, t);
		volumetricClouds.ambientLightProbeDimmer.value = Mathf.Lerp(
			startPreset.ambientLightProbeDimmer, targetPreset.ambientLightProbeDimmer, t);
		volumetricClouds.sunLightDimmer.value = Mathf.Lerp(
			startPreset.sunLightDimmer, targetPreset.sunLightDimmer, t);

		// NOVAS PROPRIEDADES
		volumetricClouds.shapeOffset.value = Vector3.Lerp(
			startPreset.shapeOffset, targetPreset.shapeOffset, t);
		volumetricClouds.shapeSpeedMultiplier.value = Mathf.Lerp(
			startPreset.shapeSpeedMultiplier, targetPreset.shapeSpeedMultiplier, t);
		volumetricClouds.erosionSpeedMultiplier.value = Mathf.Lerp(
			startPreset.erosionSpeedMultiplier, targetPreset.erosionSpeedMultiplier, t);
		volumetricClouds.altitudeDistortion.value = Mathf.Lerp(
			startPreset.altitudeDistortion, targetPreset.altitudeDistortion, t);
		volumetricClouds.verticalShapeWindSpeed.value = Mathf.Lerp(
			startPreset.verticalShapeWindSpeed, targetPreset.verticalShapeWindSpeed, t);
		volumetricClouds.verticalErosionWindSpeed.value = Mathf.Lerp(
			startPreset.verticalErosionWindSpeed, targetPreset.verticalErosionWindSpeed, t);
		volumetricClouds.erosionOcclusion.value = Mathf.Lerp(
			startPreset.erosionOcclusion, targetPreset.erosionOcclusion, t);
		volumetricClouds.scatteringTint.value = Color.Lerp(
			startPreset.scatteringTint, targetPreset.scatteringTint, t);
		volumetricClouds.powderEffectIntensity.value = Mathf.Lerp(
			startPreset.powderEffectIntensity, targetPreset.powderEffectIntensity, t);
		volumetricClouds.multiScattering.value = Mathf.Lerp(
			startPreset.multiScattering, targetPreset.multiScattering, t);

		// Shadows - valores booleanos não são interpolados, usamos step
		if (t >= 1f)
		{
			volumetricClouds.shadows.value = targetPreset.shadows;
			volumetricClouds.shadowResolution.value = targetPreset.shadowResolution;
		}

		volumetricClouds.shadowDistance.value = Mathf.Lerp(
			startPreset.shadowDistance, targetPreset.shadowDistance, t);
		volumetricClouds.shadowOpacity.value = Mathf.Lerp(
			startPreset.shadowOpacity, targetPreset.shadowOpacity, t);
		volumetricClouds.shadowOpacityFallback.value = Mathf.Lerp(
			startPreset.shadowOpacityFallback, targetPreset.shadowOpacityFallback, t);

		// Quality
		volumetricClouds.temporalAccumulationFactor.value = Mathf.Lerp(
			startPreset.temporalAccumulationFactor, targetPreset.temporalAccumulationFactor, t);
		volumetricClouds.perceptualBlending.value = Mathf.Lerp(
			startPreset.perceptualBlending, targetPreset.perceptualBlending, t);

		// Steps - inteiros, arredondamos no final
		volumetricClouds.numPrimarySteps.value = Mathf.RoundToInt(Mathf.Lerp(
			startPreset.numPrimarySteps, targetPreset.numPrimarySteps, t));
		volumetricClouds.numLightSteps.value = Mathf.RoundToInt(Mathf.Lerp(
			startPreset.numLightSteps, targetPreset.numLightSteps, t));

		// Fade In - valores booleanos/enuм não são interpolados, usamos step
		if (t >= 1f)
		{
			volumetricClouds.fadeInMode.value = targetPreset.fadeInMode;
		}

		volumetricClouds.fadeInStart.value = Mathf.Lerp(
			startPreset.fadeInStart, targetPreset.fadeInStart, t);
		volumetricClouds.fadeInDistance.value = Mathf.Lerp(
			startPreset.fadeInDistance, targetPreset.fadeInDistance, t);

		// Local Clouds - booleano, step no final
		if (t >= 1f)
		{
			volumetricClouds.localClouds.value = targetPreset.localClouds;
		}

		// Micro Erosion - booleano, step no final
		if (t >= 1f)
		{
			volumetricClouds.microErosion.value = targetPreset.useMicroErosion;
		}

		// Curves - estas precisam de uma abordagem especial
		// Para simplificar, vamos apenas copiar no final
		if (t >= 1f)
		{
			volumetricClouds.densityCurve.value = targetPreset.densityCurve;
			volumetricClouds.erosionCurve.value = targetPreset.erosionCurve;
			volumetricClouds.ambientOcclusionCurve.value = targetPreset.ambientOcclusionCurve;
		}
	}

	private void ApplyCloudPreset(CloudPresetSettings preset)
	{
		volumetricClouds.cloudPreset = VolumetricClouds.CloudPresets.Custom;

		// Propriedades existentes
		volumetricClouds.densityMultiplier.value = preset.densityMultiplier;
		volumetricClouds.shapeFactor.value = preset.shapeFactor;
		volumetricClouds.shapeScale.value = preset.shapeScale;
		volumetricClouds.erosionFactor.value = preset.erosionFactor;
		volumetricClouds.erosionScale.value = preset.erosionScale;
		volumetricClouds.bottomAltitude.value = preset.bottomAltitude;
		volumetricClouds.altitudeRange.value = preset.altitudeRange;
		volumetricClouds.microErosionFactor.value = preset.microErosionFactor;
		volumetricClouds.microErosionScale.value = preset.microErosionScale;
		volumetricClouds.globalSpeed.value = preset.globalSpeed;
		volumetricClouds.globalOrientation.value = preset.globalOrientation;
		volumetricClouds.ambientLightProbeDimmer.value = preset.ambientLightProbeDimmer;
		volumetricClouds.sunLightDimmer.value = preset.sunLightDimmer;

		// NOVAS PROPRIEDADES
		volumetricClouds.shapeOffset.value = preset.shapeOffset;
		volumetricClouds.shapeSpeedMultiplier.value = preset.shapeSpeedMultiplier;
		volumetricClouds.erosionSpeedMultiplier.value = preset.erosionSpeedMultiplier;
		volumetricClouds.altitudeDistortion.value = preset.altitudeDistortion;
		volumetricClouds.verticalShapeWindSpeed.value = preset.verticalShapeWindSpeed;
		volumetricClouds.verticalErosionWindSpeed.value = preset.verticalErosionWindSpeed;
		volumetricClouds.erosionOcclusion.value = preset.erosionOcclusion;
		volumetricClouds.scatteringTint.value = preset.scatteringTint;
		volumetricClouds.powderEffectIntensity.value = preset.powderEffectIntensity;
		volumetricClouds.multiScattering.value = preset.multiScattering;
		volumetricClouds.shadows.value = preset.shadows;
		volumetricClouds.shadowResolution.value = preset.shadowResolution;
		volumetricClouds.shadowDistance.value = preset.shadowDistance;
		volumetricClouds.shadowOpacity.value = preset.shadowOpacity;
		volumetricClouds.shadowOpacityFallback.value = preset.shadowOpacityFallback;
		volumetricClouds.temporalAccumulationFactor.value = preset.temporalAccumulationFactor;
		volumetricClouds.perceptualBlending.value = preset.perceptualBlending;
		volumetricClouds.numPrimarySteps.value = preset.numPrimarySteps;
		volumetricClouds.numLightSteps.value = preset.numLightSteps;
		volumetricClouds.fadeInMode.value = preset.fadeInMode;
		volumetricClouds.fadeInStart.value = preset.fadeInStart;
		volumetricClouds.fadeInDistance.value = preset.fadeInDistance;
		volumetricClouds.localClouds.value = preset.localClouds;
		volumetricClouds.microErosion.value = preset.useMicroErosion;

		// Curves
		volumetricClouds.densityCurve.value = preset.densityCurve;
		volumetricClouds.erosionCurve.value = preset.erosionCurve;
		volumetricClouds.ambientOcclusionCurve.value = preset.ambientOcclusionCurve;
	}
	#endregion

	#region Weather Transition Methods - MODIFICADO
	private IEnumerator TransitionRainStage(RainStageConfig targetConfig)
	{
		float rainAudio_targetVolume = currentRainIntensity == RainIntensity.Heavy ? 0.5f :
									currentRainIntensity == RainIntensity.Medium ? 0.35f : 0.2f;
		float rainAudio_startVolume = rainAudio.volume;
		float elapsedTime = 0f;

		RainStage startStage = GetCurrentRainParameters();
		CloudPresetSettings startCloudPreset = GetCurrentCloudPreset();

		while (elapsedTime < weather_state_transition_speed)
		{
			elapsedTime += Time.deltaTime;
			float t = elapsedTime / weather_state_transition_speed;

			rainAudio.volume = Mathf.Lerp(rainAudio_startVolume, rainAudio_targetVolume, t);

			// Interpola modificadores
			float fogIntensity = Mathf.Lerp(startStage.fogIntensity, targetConfig.stage.fogIntensity, t);
			float lightIntensityMod = Mathf.Lerp(startStage.lightIntensityModifier, targetConfig.stage.lightIntensityModifier, t);
			float skyboxIntensityMod = Mathf.Lerp(startStage.skyboxIntensityModifier, targetConfig.stage.skyboxIntensityModifier, t);
			float lightTemperatureMod = Mathf.Lerp(startStage.lightTemperatureModifier, targetConfig.stage.lightTemperatureModifier, t);

			float skyboxExposure = Mathf.Lerp(startStage.skyboxExposure, targetConfig.stage.skyboxExposure, t);
			Color fogColor = Color.Lerp(startStage.fogColor, targetConfig.stage.fogColor, t);

			ApplyWeatherModifiers(lightIntensityMod, skyboxIntensityMod, lightTemperatureMod,
								fogIntensity,
								skyboxExposure, fogColor);

			if (rainParticleSystem != null)
			{
				rainEmissionModule.rateOverTimeMultiplier = Mathf.Lerp(startStage.rainRate, targetConfig.stage.rainRate, t);
			}

			TransitionCloudPreset(startCloudPreset, targetConfig.cloudPreset, t);
			yield return null;
		}

		// Aplica valores finais
		ApplyWeatherModifiers(
			targetConfig.stage.lightIntensityModifier,
			targetConfig.stage.skyboxIntensityModifier,
			targetConfig.stage.lightTemperatureModifier,
			targetConfig.stage.fogIntensity,
			targetConfig.stage.skyboxExposure,
			targetConfig.stage.fogColor
		);

		if (rainParticleSystem != null)
		{
			rainEmissionModule.rateOverTimeMultiplier = targetConfig.stage.rainRate;
		}

		ApplyCloudPreset(targetConfig.cloudPreset);
		rainAudio.volume = rainAudio_targetVolume;
	}

	private IEnumerator TransitionSnowStage(SnowStageConfig targetConfig)
	{
		float elapsedTime = 0f;
		SnowStage startStage = GetCurrentSnowParameters();
		CloudPresetSettings startCloudPreset = GetCurrentCloudPreset();

		while (elapsedTime < weather_state_transition_speed)
		{
			elapsedTime += Time.deltaTime;
			float t = elapsedTime / weather_state_transition_speed;

			// Interpola modificadores
			float fogIntensity = Mathf.Lerp(startStage.fogIntensity, targetConfig.stage.fogIntensity, t);
			float lightIntensityMod = Mathf.Lerp(startStage.lightIntensityModifier, targetConfig.stage.lightIntensityModifier, t);
			float skyboxIntensityMod = Mathf.Lerp(startStage.skyboxIntensityModifier, targetConfig.stage.skyboxIntensityModifier, t);
			float lightTemperatureMod = Mathf.Lerp(startStage.lightTemperatureModifier, targetConfig.stage.lightTemperatureModifier, t);
			float skyboxExposure = Mathf.Lerp(startStage.skyboxExposure, targetConfig.stage.skyboxExposure, t);
			Color fogColor = Color.Lerp(startStage.fogColor, targetConfig.stage.fogColor, t);

			ApplyWeatherModifiers(lightIntensityMod, skyboxIntensityMod, lightTemperatureMod,
								fogIntensity,
								skyboxExposure, fogColor);

			if (snowParticleSystem != null)
			{
				var snowEmission = snowParticleSystem.emission;
				snowEmission.rateOverTimeMultiplier = Mathf.Lerp(startStage.snowRate, targetConfig.stage.snowRate, t);
			}

			TransitionCloudPreset(startCloudPreset, targetConfig.cloudPreset, t);
			yield return null;
		}

		// Aplica valores finais
		ApplyWeatherModifiers(
			targetConfig.stage.lightIntensityModifier,
			targetConfig.stage.skyboxIntensityModifier,
			targetConfig.stage.lightTemperatureModifier,
			targetConfig.stage.fogIntensity,
			targetConfig.stage.skyboxExposure,
			targetConfig.stage.fogColor
		);

		if (snowParticleSystem != null)
		{
			var snowEmission = snowParticleSystem.emission;
			snowEmission.rateOverTimeMultiplier = targetConfig.stage.snowRate;
		}

		ApplyCloudPreset(targetConfig.cloudPreset);
	}

	private IEnumerator TransitionCloudyStage(CloudyStageConfig targetConfig)
	{
		float elapsedTime = 0f;
		CloudyStage startStage = GetCurrentCloudyParameters();
		CloudPresetSettings startCloudPreset = GetCurrentCloudPreset();

		while (elapsedTime < weather_state_transition_speed)
		{
			elapsedTime += Time.deltaTime;
			float t = elapsedTime / weather_state_transition_speed;

			// Interpola modificadores
			float fogIntensity = Mathf.Lerp(startStage.fogIntensity, targetConfig.stage.fogIntensity, t);
			float lightIntensityMod = Mathf.Lerp(startStage.lightIntensityModifier, targetConfig.stage.lightIntensityModifier, t);
			float skyboxIntensityMod = Mathf.Lerp(startStage.skyboxIntensityModifier, targetConfig.stage.skyboxIntensityModifier, t);
			float lightTemperatureMod = Mathf.Lerp(startStage.lightTemperatureModifier, targetConfig.stage.lightTemperatureModifier, t);

			float skyboxExposure = Mathf.Lerp(startStage.skyboxExposure, targetConfig.stage.skyboxExposure, t);
			Color fogColor = Color.Lerp(startStage.fogColor, targetConfig.stage.fogColor, t);
			float windSpeed = Mathf.Lerp(startStage.wind_speed, targetConfig.stage.wind_speed, t);

			ApplyWeatherModifiers(lightIntensityMod, skyboxIntensityMod, lightTemperatureMod,
								fogIntensity,
								skyboxExposure, fogColor);

			if (cloudyObject != null)
			{
				var windEmission = windfParticleSystem.emission;
				windEmission.rateOverTimeMultiplier = windSpeed;
			}

			TransitionCloudPreset(startCloudPreset, targetConfig.cloudPreset, t);
			yield return null;
		}

		// Aplica valores finais
		ApplyWeatherModifiers(
			targetConfig.stage.lightIntensityModifier,
			targetConfig.stage.skyboxIntensityModifier,
			targetConfig.stage.lightTemperatureModifier,
			targetConfig.stage.fogIntensity,
			targetConfig.stage.skyboxExposure,
			targetConfig.stage.fogColor
		);

		if (cloudyObject != null)
		{
			var windEmission = windfParticleSystem.emission;
			windEmission.rateOverTimeMultiplier = targetConfig.stage.wind_speed;
		}

		ApplyCloudPreset(targetConfig.cloudPreset);
	}

	private IEnumerator TransitionClearStage(ClearStageConfig targetConfig)
	{
		float elapsedTime = 0f;
		ClearStage startStage = GetCurrentClearParameters();
		CloudPresetSettings startCloudPreset = GetCurrentCloudPreset();

		while (elapsedTime < weather_state_transition_speed)
		{
			elapsedTime += Time.deltaTime;
			float t = elapsedTime / weather_state_transition_speed;

			// Interpola modificadores
			float fogIntensity = Mathf.Lerp(startStage.fogIntensity, targetConfig.stage.fogIntensity, t);
			float lightIntensityMod = Mathf.Lerp(startStage.lightIntensityModifier, targetConfig.stage.lightIntensityModifier, t);
			float skyboxIntensityMod = Mathf.Lerp(startStage.skyboxIntensityModifier, targetConfig.stage.skyboxIntensityModifier, t);
			float lightTemperatureMod = Mathf.Lerp(startStage.lightTemperatureModifier, targetConfig.stage.lightTemperatureModifier, t);
			float skyboxExposure = Mathf.Lerp(startStage.skyboxExposure, targetConfig.stage.skyboxExposure, t);
			Color fogColor = Color.Lerp(startStage.fogColor, targetConfig.stage.fogColor, t);

			ApplyWeatherModifiers(lightIntensityMod, skyboxIntensityMod, lightTemperatureMod,
								fogIntensity,
								skyboxExposure, fogColor);

			TransitionCloudPreset(startCloudPreset, targetConfig.cloudPreset, t);
			yield return null;
		}

		// Aplica valores finais
		ApplyWeatherModifiers(
			targetConfig.stage.lightIntensityModifier,
			targetConfig.stage.skyboxIntensityModifier,
			targetConfig.stage.lightTemperatureModifier,
			targetConfig.stage.fogIntensity,
			targetConfig.stage.skyboxExposure,
			targetConfig.stage.fogColor
		);

		ApplyCloudPreset(targetConfig.cloudPreset);
	}
	#endregion

	#region Weather Spawn Methods
	void SpawnRain()
	{
		if (!rain)
		{
			Array values = Enum.GetValues(typeof(RainIntensity));
			currentRainIntensity = (RainIntensity)values.GetValue(UnityEngine.Random.Range(1, values.Length));

			RainStageConfig selectedConfig = GetCurrentRainStageConfig();

			print(currentRainIntensity);

			rain = true;
			rainObject.SetActive(true);
			StartCoroutine(TransitionRainStage(selectedConfig));

			// Desativa outros efeitos climáticos
			if (snow) StartCoroutine(DisableSnow());
			if (cloudy) StartCoroutine(DisableCloudy());
			if (sunny) StartCoroutine(DisableSunny());
		}
	}

	private float GetSkyboxExposure()
	{
		if (skyboxMaterial == null) return 1f;

		if (skyboxMaterial.HasProperty("_Exposure"))
			return skyboxMaterial.GetFloat("_Exposure");

		return 1f;
	}

	void SpawnSnow()
	{
		if (!snow)
		{
			Array values = Enum.GetValues(typeof(SnowIntensity));
			currentSnowIntensity = (SnowIntensity)values.GetValue(UnityEngine.Random.Range(1, values.Length));

			SnowStageConfig selectedConfig = GetCurrentSnowStageConfig();

			print(currentSnowIntensity);

			snow = true;
			snowObject.SetActive(true);
			StartCoroutine(TransitionSnowStage(selectedConfig));

			if (rain) StartCoroutine(DisableRain());
			if (cloudy) StartCoroutine(DisableCloudy());
			if (sunny) StartCoroutine(DisableSunny());
		}
	}

	void SpawnCloudy()
	{
		if (!cloudy)
		{
			// Determina a intensidade baseada no tipo de clima
			if (currentWeather == "partly-cloudy-day" || currentWeather == "partly-cloudy-night")
			{
				currentCloudyIntensity = CloudyIntensity.PartlyCloudy;
			}
			else if (currentWeather == "fog")
			{
				currentCloudyIntensity = CloudyIntensity.Overcast;
			}
			else
			{
				Array values = Enum.GetValues(typeof(CloudyIntensity));
				currentCloudyIntensity = (CloudyIntensity)values.GetValue(UnityEngine.Random.Range(1, values.Length));
			}

			CloudyStageConfig selectedConfig = GetCurrentCloudyStageConfig();

			print(currentCloudyIntensity);

			cloudy = true;
			cloudyObject.SetActive(true);
			StartCoroutine(TransitionCloudyStage(selectedConfig));

			if (snow) StartCoroutine(DisableSnow());
			if (rain) StartCoroutine(DisableRain());
			if (sunny) StartCoroutine(DisableSunny());
		}
	}

	void SpawnClearWeather()
	{
		if (!sunny)
		{
			ClearStageConfig selectedConfig = GetCurrentClearStageConfig();

			if (currentWeather == "clear-night")
			{
				currentClearIntensity = ClearIntensity.ClearNight;
			}
			else
			{
				currentClearIntensity = ClearIntensity.ClearDay;
			}

			print(currentClearIntensity);

			sunny = true;

			StartCoroutine(TransitionClearStage(selectedConfig));

			// Desativa outros efeitos climáticos
			if (snow) StartCoroutine(DisableSnow());
			if (cloudy) StartCoroutine(DisableCloudy());
			if (rain) StartCoroutine(DisableRain());
		}
	}
	#endregion

	#region Weather Disable Methods - MODIFICADO
	IEnumerator DisableRain()
	{
		rain = false;
		currentRainIntensity = RainIntensity.None;

		if (rainObject != null)
		{
			ParticleSystem ps = rainObject.GetComponent<ParticleSystem>();
			if (ps != null) ps.Stop();

			Animator anim = rainObject.GetComponent<Animator>();
			if (anim != null) anim.Play("rain_exit");

			// Apenas remove os modificadores - os valores base do ciclo dia/noite continuam aplicados
			ApplyDayNightCycleBaseValues();

			yield return new WaitForSeconds(5);
			rainObject.SetActive(false);
		}
	}

	IEnumerator DisableSnow()
	{
		snow = false;
		currentSnowIntensity = SnowIntensity.None;

		if (snowObject != null)
		{
			ParticleSystem ps = snowObject.GetComponent<ParticleSystem>();
			if (ps != null) ps.Stop();

			Animator anim = snowObject.GetComponent<Animator>();
			if (anim != null) anim.Play("snow_exit");

			// Apenas remove os modificadores
			ApplyDayNightCycleBaseValues();

			yield return new WaitForSeconds(5);
			snowObject.SetActive(false);
		}
	}

	IEnumerator DisableCloudy()
	{
		cloudy = false;
		currentCloudyIntensity = CloudyIntensity.None;

		if (cloudyObject != null)
		{
			Animator anim = cloudyObject.GetComponent<Animator>();
			if (anim != null) anim.Play("cloudy_exit");

			// Apenas remove os modificadores
			ApplyDayNightCycleBaseValues();

			yield return new WaitForSeconds(5);
			cloudyObject.SetActive(false);
		}
	}

	IEnumerator DisableSunny()
	{
		sunny = false;
		currentClearIntensity = ClearIntensity.None;

		// Apenas remove os modificadores (que já são neutros para clima claro)
		ApplyDayNightCycleBaseValues();

		yield return new WaitForSeconds(5);
	}
	#endregion

	#region Cloud Preset Settings
	[Serializable]

	public class CloudPresetSettings
	{
		public string presetName;

		// General
		public bool localClouds = false;

		// Shape (na mesma ordem do VolumetricClouds.cs)
		[Header("Shape Properties")]
		public float densityMultiplier = 0.4f;
		public AnimationCurve densityCurve;
		public float shapeFactor = 0.9f;
		public float shapeScale = 5.0f;
		public float erosionFactor = 0.8f;
		public float erosionScale = 107.0f;
		public AnimationCurve erosionCurve;
		public AnimationCurve ambientOcclusionCurve;
		public bool useMicroErosion = false;
		public float microErosionFactor = 0.5f;
		public float microErosionScale = 200.0f;
		public float bottomAltitude = 1200.0f;
		public float altitudeRange = 2000.0f;
		public Vector3 shapeOffset = Vector3.zero;

		// Wind
		[Header("Wind Properties")]
		public float globalSpeed = 0.0f;
		public float globalOrientation = 0.0f;
		public float shapeSpeedMultiplier = 1.0f;
		public float erosionSpeedMultiplier = 0.25f;
		public float altitudeDistortion = 0.25f;
		public float verticalShapeWindSpeed = 0.0f;
		public float verticalErosionWindSpeed = 0.0f;

		// Lighting
		[Header("Lighting Properties")]
		public float ambientLightProbeDimmer = 1.0f;
		public float sunLightDimmer = 1.0f;
		public float erosionOcclusion = 0.1f;
		public Color scatteringTint = new Color(0.0f, 0.0f, 0.0f, 1.0f);
		public float powderEffectIntensity = 0.25f;
		public float multiScattering = 0.5f;

		// Shadows
		[Header("Shadow Properties")]
		public bool shadows = false;
		public CloudShadowResolution shadowResolution = CloudShadowResolution.Medium256;
		public float shadowDistance = 8000.0f;
		public float shadowOpacity = 1.0f;
		public float shadowOpacityFallback = 0.0f;

		// Quality
		[Header("Quality Properties")]
		public float temporalAccumulationFactor = 0.95f;
		public float perceptualBlending = 1.0f;
		public int numPrimarySteps = 32;
		public int numLightSteps = 2;
		public CloudFadeInMode fadeInMode = CloudFadeInMode.Automatic;
		public float fadeInStart = 0.0f;
		public float fadeInDistance = 5000.0f;

		public CloudPresetSettings()
		{
			// Inicializa as curves com valores padrão
			densityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1.0f), new Keyframe(1.0f, 0.1f));
			erosionCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f));
			ambientOcclusionCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.4f), new Keyframe(1.0f, 0.0f));
		}

		public CloudPresetSettings(string name)
		{
			presetName = name;

			// Inicializa as curves com valores padrão
			densityCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1.0f), new Keyframe(1.0f, 0.1f));
			erosionCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f));
			ambientOcclusionCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.4f), new Keyframe(1.0f, 0.0f));
		}
	}
	#endregion
}