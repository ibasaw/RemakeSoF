using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;

using SoF2Remake.Utils;
using SoF2Remake.Data;

[DisallowMultipleComponent]
public class PlayerSoundSystem : MonoBehaviour
{
    [Header("Sound System")]
    [SerializeField] private bool drawSoundSystemDebugGUI = false;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private float landingSoundVolume = 1f;
    [SerializeField] private float footstepSoundVolume = 1f;
    [SerializeField] private float weaponSoundVolume = 1f;
    [SerializeField] private bool enableLandingSounds = true;
    [SerializeField] private bool enableFootstepSounds = true;
    [SerializeField] private bool enableWeaponSounds = true;
    [SerializeField] private float firstFootstepDelayMs = 100f; // ms

    // AudioSources (created if missing)
    private AudioSource landingSoundSource;
    private AudioSource footstepSoundSource;
    private AudioSource weaponSoundSource;

    // Footstep playback control
	private Dictionary<string, int> footstepNextIndexByMaterial = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	// Footstep sound completion tracking
	private bool isFootstepSoundPlaying = false;
	private float lastFootstepSoundTime = 0f;
	private float currentFootstepSoundDuration = 0f;
	private float movementStartTime = 0f; // Time when movement started
	private bool hasPlayedFirstFootstep = false; // Track if first footstep after movement start has been played

	// Weapon sound playback control
	private bool isWeaponSoundPlaying = false;
	private float lastWeaponSoundTime = 0f;
	private float currentSoundDuration = 0f;

	// Sound cache for different surface materials
	private Dictionary<string, MaterialInfo> materialInfos = new Dictionary<string, MaterialInfo>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, AudioClip> landingSounds = new Dictionary<string, AudioClip>();
	private Dictionary<string, AudioClip[]> footstepSounds = new Dictionary<string, AudioClip[]>();
	private Dictionary<string, AudioClip[]> weaponSounds = new Dictionary<string, AudioClip[]>();

	private bool soundsLoaded = false;

    private void OnGUICustom()
    {
		if (drawSoundSystemDebugGUI && landingSoundSource != null && footstepSoundSource != null && weaponSoundSource != null)
		{
            float scaleFactor = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scaleFactor, scaleFactor, 1f));

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };
            GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                normal = { textColor = Color.white }
            };

            float x = 10f, y = 10f, line = 24f;
			GUI.Label(new Rect(x, y, 600, line), $"Sound System Loaded: {soundsLoaded}", headerStyle); y += line * 1.2f;
			GUI.Label(new Rect(x, y, 600, line), $"SFX Group: {(sfxGroup != null ? sfxGroup.name : "None")}", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Landing enabled: {enableLandingSounds} Footstep enabled: {enableFootstepSounds} Weapon enabled: {enableWeaponSounds}", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Landing Volume: {landingSoundVolume:F2} Footstep Volume: {footstepSoundVolume:F2} Weapon Volume: {weaponSoundVolume:F2}", valueStyle); y += line;
			GUI.Label(new Rect(x, y, 600, line), $"Footstep Delay: {firstFootstepDelayMs}ms (First played: {hasPlayedFirstFootstep})", valueStyle); y += line;
		}
    }

    public void PlayLandingSound(RaycastHit hit){
        // Play landing sound based on ground material (once) - only if not higher
        if (enableLandingSounds && soundsLoaded)
        {
            string materialType = GetGroundMaterialType(hit);
            PlayLandingSoundForMaterialType(materialType);
        }
    }

    /// <summary>
	/// Play landing sound based on ground material for a specific material surface
	/// </summary>
	private void PlayLandingSoundForMaterialType(string materialType)
	{
		if (!enableLandingSounds || !soundsLoaded || landingSoundSource == null)
			return;

		if (landingSounds.TryGetValue(materialType, out AudioClip clip))
		{
			if (clip == null) return;

			// Set mixer group if available
			if (sfxGroup != null)
			{
				landingSoundSource.outputAudioMixerGroup = sfxGroup;
			}
			else
			{
				landingSoundSource.outputAudioMixerGroup = null;
			}

			landingSoundSource.PlayOneShot(clip, landingSoundVolume);
		}
	}

    /// <summary>
    /// Play footstep sound cycling through step clips per material for a specific material surface
    /// </summary>
    private void PlayFootstepSound(string materialType)
    {
        if (!enableFootstepSounds || !soundsLoaded || footstepSoundSource == null)
            return;

        if (!footstepSounds.TryGetValue(materialType, out AudioClip[] clips) || clips == null || clips.Length == 0)
            return;

        if (!footstepNextIndexByMaterial.TryGetValue(materialType, out int nextIndex))
            nextIndex = 0;

        int safeIndex = 0;
        if (clips.Length > 0)
        {
            safeIndex = Mathf.Abs(nextIndex) % clips.Length;
        }

        AudioClip clip = clips[safeIndex];
        if (clip == null) return;

        // Set mixer group if available
        if (sfxGroup != null)
        {
            footstepSoundSource.outputAudioMixerGroup = sfxGroup;
        }
        else
        {
            footstepSoundSource.outputAudioMixerGroup = null;
        }

        footstepSoundSource.PlayOneShot(clip, footstepSoundVolume);
        footstepNextIndexByMaterial[materialType] = safeIndex + 1;
    }

    /// <summary>
    /// Play footstep sound and return the duration of the played sound
    /// </summary>
    private float PlayFootstepSoundWithDuration(string materialType)
    {
        if (!enableFootstepSounds || !soundsLoaded || footstepSoundSource == null)
            return 0f;

        if (!footstepSounds.TryGetValue(materialType, out AudioClip[] clips) || clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[PlayFootstepSoundWithDuration] No footstep sounds found for material '{materialType}'");
            return 0f;
        }

        if (!footstepNextIndexByMaterial.TryGetValue(materialType, out int nextIndex))
            nextIndex = 0;

        int safeIndex = 0;
        if (clips.Length > 0)
        {
            safeIndex = Mathf.Abs(nextIndex) % clips.Length;
        }

        AudioClip clip = clips[safeIndex];
        if (clip == null) return 0f;

        // Set mixer group if available
        if (sfxGroup != null)
        {
            footstepSoundSource.outputAudioMixerGroup = sfxGroup;
        }
        else
        {
            footstepSoundSource.outputAudioMixerGroup = null;
        }

        footstepSoundSource.PlayOneShot(clip, footstepSoundVolume);
        footstepNextIndexByMaterial[materialType] = safeIndex + 1;
        
        //Debug.Log($"[PlayFootstepSoundWithDuration] Playing footstep for '{materialType}' - {clip.name} (Duration: {clip.length:F2}s)");
        return clip.length; // Return the actual duration of the sound
    }

    /// <summary>
	/// Get the material type from the ground mesh's shader_file property
	/// </summary>
	public string GetGroundMaterialType(RaycastHit hit)
	{
		string detectedMaterial = "concrete"; // Default
		string detectionMethod = "default";		

		// Try to get Ghoul2Meta component from the hit object
		if (hit.collider.TryGetComponent<Ghoul2Meta>(out var meta))
		{
			// Try to get shader_file property (using new dynamic API)
			string q3MapMaterialName = meta.Q3MapMaterial; // This uses the convenience property
			if (!string.IsNullOrEmpty(q3MapMaterialName) && landingSounds.ContainsKey(q3MapMaterialName))
			{
				detectedMaterial = q3MapMaterialName;
				detectionMethod = $"q3MapMaterialName '{q3MapMaterialName}' found in landingSounds";
			}
		}
		else
		{
			detectionMethod = $"no Ghoul2Meta, using default for object name: '{hit.collider.gameObject.name}";
		}
		//Debug.Log($"Ground material detected as '{detectedMaterial}' via {detectionMethod}");
		return detectedMaterial;
	}

    /// <summary>
    /// Handles footstep sound playback each frame.
    /// Call this from PlayerController.Update().
    /// </summary>
    /// <param name="isWalking">Ob der Spieler gerade läuft</param>
    /// <param name="lastGroundHit">Der letzte Ground Hit</param>
    public void HandleFootstepSoundEvents(bool isWalking, RaycastHit lastGroundHit)
    {
        // Track movement start time
		if (isWalking && !hasPlayedFirstFootstep)
		{
			if (movementStartTime == 0f)
			{
				movementStartTime = Time.time;
			}
		}
		else if (!isWalking)
		{
			// Reset movement tracking when not walking
			movementStartTime = 0f;
			hasPlayedFirstFootstep = false;
		}

		// Footstep sound - play next sound when current one finishes
		if (enableFootstepSounds && soundsLoaded && isWalking && footstepSoundSource != null)
		{
			// Check if we need to start a new footstep sound
			if (!isFootstepSoundPlaying)
			{
				// For the first footstep, check if enough delay has passed
				if (!hasPlayedFirstFootstep)
				{
					float delayInSeconds = firstFootstepDelayMs / 1000f;
					if (Time.time - movementStartTime >= delayInSeconds)
					{
						string materialType = GetGroundMaterialType(lastGroundHit);
						currentFootstepSoundDuration = PlayFootstepSoundWithDuration(materialType);
						isFootstepSoundPlaying = true;
						lastFootstepSoundTime = Time.time;
						hasPlayedFirstFootstep = true;
					}
				}
				else
				{
					// For subsequent footsteps, play immediately
					string materialType = GetGroundMaterialType(lastGroundHit);
					currentFootstepSoundDuration = PlayFootstepSoundWithDuration(materialType);
					isFootstepSoundPlaying = true;
					lastFootstepSoundTime = Time.time;
				}
			}
			else
			{
				// Check if enough time has passed for the sound to finish
				if (Time.time - lastFootstepSoundTime >= currentFootstepSoundDuration)
				{
					isFootstepSoundPlaying = false;
				}
			}
		}
		else
		{
			// Reset when not walking
			isFootstepSoundPlaying = false;
		}
    }

    /// <summary>
    /// Handles weapon-sound playback each frame.
    /// Call this from PlayerController.Update().
    /// </summary>
    /// <param name="isAttacking">Ob der Spieler gerade angreift</param>
    /// <param name="weaponName">z. B. "Knife"</param>
    /// <param name="soundType">z. B. "swing", "hit", "reload"</param>
    public void HandleWeaponSoundEvents(bool isAttacking, string weaponName, string soundType)
    {
        // Weapon sound - play next sound when current one finishes
		if (isAttacking && enableWeaponSounds)
		{
			// Check if we need to start a new sound
			if (!isWeaponSoundPlaying)
			{
				currentSoundDuration = PlayWeaponSoundWithDuration(weaponName, soundType);
				isWeaponSoundPlaying = true;
				lastWeaponSoundTime = Time.time;
			}
			else
			{
				// Check if enough time has passed for the sound to finish
				if (Time.time - lastWeaponSoundTime >= currentSoundDuration)
				{
					isWeaponSoundPlaying = false;
				}
			}
		}
		else
		{
			// Reset when not attacking
			isWeaponSoundPlaying = false;
		}
    }

    /// <summary>
    /// Play weapon sound cycling through sound clips for the specified weapon and sound type
    /// </summary>
    private void PlayWeaponSound(string weaponName, string soundType)
    {
        if (!enableWeaponSounds || !soundsLoaded || weaponSoundSource == null)
            return;

        string soundKey = $"{weaponName}_{soundType}";
        if (!weaponSounds.TryGetValue(soundKey, out AudioClip[] clips) || clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[PlayWeaponSound] No sounds found for {soundKey}");
            return;
        }

        // Play a random sound from the available clips
        int randomIndex = UnityEngine.Random.Range(0, clips.Length);
        AudioClip clip = clips[randomIndex];
        if (clip == null) return;

        // Set mixer group if available
        if (sfxGroup != null)
        {
            weaponSoundSource.outputAudioMixerGroup = sfxGroup;
        }
        else
        {
            weaponSoundSource.outputAudioMixerGroup = null;
        }

        weaponSoundSource.PlayOneShot(clip, weaponSoundVolume);
        //Debug.Log($"[PlayWeaponSound] Playing {soundKey} - {clip.name}");
    }

    /// <summary>
    /// Play weapon sound and return the duration of the played sound
    /// </summary>
    private float PlayWeaponSoundWithDuration(string weaponName, string soundType)
    {
        if (!enableWeaponSounds || !soundsLoaded || weaponSoundSource == null)
            return 0f;

        string soundKey = $"{weaponName}_{soundType}";
        if (!weaponSounds.TryGetValue(soundKey, out AudioClip[] clips) || clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[PlayWeaponSoundWithDuration] No sounds found for {soundKey}");
            return 0f;
        }

        // Play a random sound from the available clips
        int randomIndex = UnityEngine.Random.Range(0, clips.Length);
        AudioClip clip = clips[randomIndex];
        if (clip == null) return 0f;

        // Set mixer group if available
        if (sfxGroup != null)
        {
            weaponSoundSource.outputAudioMixerGroup = sfxGroup;
        }
        else
        {
            weaponSoundSource.outputAudioMixerGroup = null;
        }

        weaponSoundSource.PlayOneShot(clip, weaponSoundVolume);
        //Debug.Log($"[PlayWeaponSoundWithDuration] Playing {soundKey} - {clip.name} (Duration: {clip.length:F2}s)");
        
        return clip.length; // Return the actual duration of the sound
    }


    /// <summary>
	/// Initialize the sound system
	/// </summary>
	public void InitializeSoundSystem()
	{
		if (!enableLandingSounds) return;

		// Create AudioSource if not assigned
		if (landingSoundSource == null)
		{
			landingSoundSource = gameObject.GetComponent<AudioSource>();
			if (landingSoundSource == null)
			{
				landingSoundSource = gameObject.AddComponent<AudioSource>();
			}
		}

		if (footstepSoundSource == null)
		{
			footstepSoundSource = gameObject.GetComponent<AudioSource>();
			if (footstepSoundSource == null)
			{
				footstepSoundSource = gameObject.AddComponent<AudioSource>();
			}
		}

		if (weaponSoundSource == null)
		{
			weaponSoundSource = gameObject.GetComponent<AudioSource>();
			if (weaponSoundSource == null)
			{
				weaponSoundSource = gameObject.AddComponent<AudioSource>();
			}
		}

		ConfigureAudioSource(landingSoundSource);
		ConfigureAudioSource(footstepSoundSource);
		ConfigureAudioSource(weaponSoundSource);

		// Set MixerGroup if assigned
		if (sfxGroup != null)
		{
			landingSoundSource.outputAudioMixerGroup = sfxGroup;
			footstepSoundSource.outputAudioMixerGroup = sfxGroup;
			weaponSoundSource.outputAudioMixerGroup = sfxGroup;
		}

		LoadPlayerSounds();
	}

    private void ConfigureAudioSource(AudioSource audioSource)
    {
        // Configure AudioSource for MAXIMUM compatibility
		/*audioSource.volume = 1.0f; // Full volume
		audioSource.pitch = 1.0f;
		audioSource.spatialBlend = 0.0f; // 2D sound (always audible)
		audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
		audioSource.minDistance = 1f;
		audioSource.maxDistance = 500f;
		audioSource.playOnAwake = false;
		audioSource.loop = false;
		audioSource.mute = false;
		audioSource.enabled = true;
		audioSource.priority = 128;*/
    }

    /// <summary>
	/// Load all sounds from the uQuake/sound/player/jumps/ uQuake/sound/player/steps/ directory
	/// </summary>
	private void LoadPlayerSounds()
	{
		LoadSounds();
		LoadWeaponSounds("Knife"); // Load Weapon sounds (here just Knife)
	}

    /// <summary>
    /// Load all sounds for a specific weapon from the SoF2_Weapons.json file
    /// </summary>
    private void LoadWeaponSounds(string weaponName)
	{
		if (string.IsNullOrEmpty(weaponName))
		{
			Debug.LogWarning("[LoadWeaponSounds] Weapon name is null or empty!");
			return;
		}

        string json = JsonDataReader.TryLoadJsonText("SoF2_Weapons");
		if (string.IsNullOrEmpty(json))
		{
			Debug.Log("[LoadWeaponSounds] Keine SoF2_Weapons.json gefunden!");
			return;
		}

		JArray weaponsArray;
		try
		{
			weaponsArray = JArray.Parse(json);
		}
		catch (Exception ex)
		{
			Debug.LogError("[LoadWeaponSounds] JSON Parse Error: " + ex);
			return;
		}

		// Find the specific weapon
		JObject targetWeapon = null;
		foreach (JObject weaponObj in weaponsArray)
		{
			string currentWeaponName = weaponObj.Value<string>("name");
			if (string.Equals(currentWeaponName, weaponName, StringComparison.OrdinalIgnoreCase))
			{
				targetWeapon = weaponObj;
				break;
			}
		}

		if (targetWeapon == null)
		{
			Debug.LogWarning($"[LoadWeaponSounds] Weapon '{weaponName}' not found in SoF2_Weapons.json!");
			return;
		}

		JObject soundsObj = targetWeapon.Value<JObject>("sounds");
		if (soundsObj == null)
		{
			Debug.LogWarning($"[LoadWeaponSounds] No sounds block found for weapon '{weaponName}'!");
			return;
		}

		// Load sounds for each key in the sounds block (ready, swing, toss, etc.)
		foreach (var soundKeyProp in soundsObj.Properties())
		{
			string soundKey = soundKeyProp.Name; // e.g., "ready", "swing", "toss"
			JObject soundKeyObj = soundKeyProp.Value as JObject;
			if (soundKeyObj == null) continue;

			// Create dictionary key: "weaponName_soundKey"
			string dictionaryKey = $"{weaponName}_{soundKey}";
			
			// Collect all sound files for this key (sound1, sound2, sound3, etc.)
			var soundFiles = new List<string>();
			foreach (var soundProp in soundKeyObj.Properties())
			{
				if (soundProp.Value.Type == JTokenType.String)
				{
					string soundPath = soundProp.Value.ToString();
					soundFiles.Add(soundPath);
				}
			}

			if (soundFiles.Count == 0)
			{
				Debug.LogWarning($"[LoadWeaponSounds] No sound files found for {dictionaryKey}");
				continue;
			}

			// Load AudioClips for each sound file
			var audioClips = new List<AudioClip>();
			foreach (string soundFile in soundFiles)
			{
				AudioClip clip = TryLoadWeaponSoundClip(soundFile);
				if (clip != null)
				{
					audioClips.Add(clip);
				}
			}

			if (audioClips.Count > 0)
			{
				weaponSounds[dictionaryKey] = audioClips.ToArray();
				//Debug.Log($"[LoadWeaponSounds] Loaded {audioClips.Count} sounds for {dictionaryKey}");
			}
			else
			{
				Debug.LogWarning($"[LoadWeaponSounds] No valid AudioClips found for {dictionaryKey}");
			}
		}

		Debug.Log($"[LoadWeaponSounds] Loaded {weaponSounds.Count} sound types for weapon '{weaponName}'.");
	}
    
    private AudioClip TryLoadWeaponSoundClip(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return null;

        // Kandidatenliste für Waffen-Sounds
        var candidates = new List<string>
        {
            "uQuake/" + soundName
        };

        foreach (var c in candidates)
        {
            if (string.IsNullOrEmpty(c)) continue;
            string resourcePath = c.TrimStart('/', '\\');
            resourcePath = Path.ChangeExtension(resourcePath, null).Replace('\\', '/');
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null) return clip;
        }
        return null;
    }

    private AudioClip[] TryLoadWeaponSoundsFromFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return null;

        // Convert folder path to Resources path
        string resourcePath = folderPath.TrimStart('/', '\\');
        resourcePath = "uQuake/" + resourcePath;
        resourcePath = resourcePath.Replace('\\', '/');

        // Load all AudioClips from the folder
        AudioClip[] allClips = Resources.LoadAll<AudioClip>(resourcePath);
        if (allClips == null || allClips.Length == 0)
        {
            Debug.LogWarning($"[TryLoadWeaponSoundsFromFolder] No AudioClips found in folder '{resourcePath}'");
            return null;
        }

        // Filter out null clips and return valid ones
        var validClips = new List<AudioClip>();
        foreach (AudioClip clip in allClips)
        {
            if (clip != null)
            {
                validClips.Add(clip);
            }
        }

        Debug.Log($"[TryLoadWeaponSoundsFromFolder] Found {validClips.Count} valid AudioClips in folder '{resourcePath}'");
        return validClips.Count > 0 ? validClips.ToArray() : null;
    }

    /// <summary>
    /// Load all sounds from Data/SoF2_data_per_surface.json (falls vorhanden), 
    /// ansonsten fallback auf uQuake/sound/player/jumps/{material} uQuake/sound/player/steps/{material}
    /// Zusätzlich werden optionale Material-Eigenschaften (loudness, density, projectileBounce, friction, damage) eingelesen.
    /// </summary>
    private void LoadSounds()
    {
        landingSounds.Clear();
        footstepSounds.Clear();
        weaponSounds.Clear(); //weapons will be loaded after in LoadWeaponSounds
        materialInfos.Clear();

        string json = JsonDataReader.TryLoadJsonText("SoF2_data_per_surface");
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[LoadSounds] Keine JSON-Datei für Sounds gefunden!");
            return;
        }

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[LoadSounds] JSON Parse Error: " + ex);
            return;
        }

        foreach (var prop in root.Properties())
        {
            string materialName = prop.Name;
            JObject matObj = prop.Value as JObject;
            if (matObj == null)
            {
                // falls Wert kein Objekt ist, überspringen
                continue;
            }

            var info = new MaterialInfo
            {
                loudness = TryGetDouble(matObj, "loudness"),
                density = TryGetDouble(matObj, "density"),
                projectileBounce = TryGetDouble(matObj, "projectileBounce"),
                friction = TryGetDouble(matObj, "friction"),
                damage = TryGetDouble(matObj, "damage")
            };
            materialInfos[materialName] = info;

            // land.sound extrahieren (flexibel)
            string landSound = null;
            JObject land = matObj.Value<JObject>("land");
            if (land != null)
            {
                JToken soundTok;
                if (land.TryGetValue("sound", StringComparison.OrdinalIgnoreCase, out soundTok) && soundTok.Type == JTokenType.String)
                {
                    landSound = soundTok.ToString();
                }
                else
                {
                    // fallback: nimm das erste string-Feld in land (manche Exporte haben unkonventionelle Struktur)
                    foreach (var lp in land.Properties())
                    {
                        if (lp.Value.Type == JTokenType.String)
                        {
                            landSound = lp.Value.ToString();
                            break;
                        }
                    }
                }
            }
            string footstepSound = null;
            JObject footstep = matObj.Value<JObject>("footstep");
            if (footstep != null)
            {
                JToken soundTok;
                if (footstep.TryGetValue("sound", StringComparison.OrdinalIgnoreCase, out soundTok) && soundTok.Type == JTokenType.String)
                {
                    footstepSound = soundTok.ToString();
                }
                else
                {
                    // fallback: nimm das erste string-Feld in land (manche Exporte haben unkonventionelle Struktur)
                    foreach (var lp in footstep.Properties())
                    {
                        if (lp.Value.Type == JTokenType.String)
                        {
                            footstepSound = lp.Value.ToString();
                            break;
                        }
                    }
                }
            }
            // manchmal steht sound direkt auf oberer Ebene
            if (string.IsNullOrEmpty(landSound))
            {
                if (matObj.TryGetValue("sound", StringComparison.OrdinalIgnoreCase, out JToken sndTok) && sndTok.Type == JTokenType.String)
                {
                    landSound = sndTok.ToString();
                }
            }
            if (string.IsNullOrEmpty(footstepSound))
            {
                if (matObj.TryGetValue("sound", StringComparison.OrdinalIgnoreCase, out JToken sndTok) && sndTok.Type == JTokenType.String)
                {
                    footstepSound = sndTok.ToString();
                }
            }
            TryLoadLandingAudioClip(materialName, landSound);
            TryLoadFootstepAudioClip(materialName, footstepSound);
        }
        soundsLoaded = true;
        Debug.Log($"[LoadSounds] {landingSounds.Count} land-sounds, {footstepSounds.Count} footstep-sounds, materialInfos: {materialInfos.Count}");
    }
    
    // JSON loading moved to Utils.JsonDataReader

    /// <summary>
    /// Try to get a double value from a JObject
    /// </summary>
    private double? TryGetDouble(JObject obj, string key)
    {
        if (obj == null) return null;
        if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken tok))
        {
            if (tok.Type == JTokenType.Float || tok.Type == JTokenType.Integer)
                return tok.Value<double>();
            if (tok.Type == JTokenType.String)
            {
                if (double.TryParse(tok.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                    return v;
            }
        }
        return null;
    }

    /// <summary>
    /// Try to load a footstep audio clip from a sound name for a specific material surface
    /// </summary>
    private void TryLoadFootstepAudioClip(string materialName, string soundName)
    {
        AudioClip[] clips = null;
        if (!string.IsNullOrEmpty(soundName))
        {
            clips = TryLoadFootstepClipsFromCandidates(soundName);
        }

        if (clips != null)
        {
            footstepSounds[materialName] = clips;
            //Debug.Log($"[LoadFootstepAudioClip] Loaded '{materialName}' -> {clips}");
        }
        else
        {
            // optional: nur warnen, nicht spammen
            //Debug.LogWarning($"[LoadFootstepAudioClip] Kein Clip für '{materialName}' gefunden (soundName='{soundName}')");
        }
    }

    /// <summary>
    /// Try to load a landing audio clip from a sound name for a specific material surface
    /// </summary>
    private void TryLoadLandingAudioClip(string materialName, string soundName)
    {
        AudioClip clip = null;

        if (!string.IsNullOrEmpty(soundName))
        {
            clip = TryLoadLandingClipFromCandidates(soundName);
        }

        if (clip != null)
        {
            landingSounds[materialName] = clip;
            //Debug.Log($"[LoadLandingAudioClip] Loaded '{materialName}' -> {clip.name}");
        }
        else
        {
            // optional: nur warnen, nicht spammen
            // Debug.LogWarning($"[LoadLandingAudioClip] Kein Clip für '{materialName}' gefunden (soundName='{soundName}')");
        }
    }

    /// <summary>
    /// Try to load a footstep audio clip from a sound name for a specific material surface
    /// </summary>
    private AudioClip[] TryLoadFootstepClipsFromCandidates(string soundName){
        if (string.IsNullOrEmpty(soundName)) return null;

        // Baue Kandidaten-Basisnamen analog zur Landing-Variante
        var baseCandidates = new List<string>
        {
            "uQuake/" + soundName
        };

        var collected = new List<AudioClip>();
        const int maxPerBase = 3; // Sicherheitslimit max 3 footstep sounds pro Basis

        foreach (var baseName in baseCandidates)
        {
            if (string.IsNullOrEmpty(baseName)) continue;

            // Lade alle Clips im Zielordner und filtere per Prefix (z.B. "gravel")
            string candidate = baseName.TrimStart('/', '\\');
            candidate = Path.ChangeExtension(candidate, null).Replace('\\', '/');
            string directoryPath = Path.GetDirectoryName(candidate)?.Replace('\\', '/');
            string prefix = Path.GetFileName(candidate);
            if (string.IsNullOrEmpty(directoryPath) || string.IsNullOrEmpty(prefix)) continue;

            var allInFolder = Resources.LoadAll<AudioClip>(directoryPath) ?? Array.Empty<AudioClip>();
            if (allInFolder.Length == 0) continue;

            // Filtere alle, die mit Prefix beginnen und eine numerische Endung besitzen (prefix + number)
            var matching = new List<(AudioClip clip, int index)>();
            foreach (var c in allInFolder)
            {
                if (c == null || string.IsNullOrEmpty(c.name)) continue;
                if (!c.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                string suffix = c.name.Substring(prefix.Length);
                if (int.TryParse(suffix, out int idx))
                {
                    matching.Add((c, idx));
                }
            }

            if (matching.Count == 0) continue;

            // Sortiere nach Index und begrenze auf maxPerBase
            matching.Sort((a, b) => a.index.CompareTo(b.index));
            for (int i = 0; i < matching.Count && i < maxPerBase; i++)
            {
                collected.Add(matching[i].clip);
            }
        }

        return collected.Count > 0 ? collected.ToArray() : null;
    }

    /// <summary>
    /// Try to load a landing audio clip from a sound name for a specific material surface
    /// </summary>
    private AudioClip TryLoadLandingClipFromCandidates(string soundName)
    {
        // Kandidatenliste — passe an deine Projektstruktur an
        var candidates = new List<string>
        {
            "uQuake/" + soundName
        };

        foreach (var c in candidates)
        {
            if (string.IsNullOrEmpty(c)) continue;
            string resourcePath = c.TrimStart('/', '\\');
            resourcePath = Path.ChangeExtension(resourcePath, null).Replace('\\', '/');
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null) return clip;
        }
        return null;
    }
}