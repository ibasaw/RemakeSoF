using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkeletonLoaderUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown skeletonDropdown;
    public Button loadButton;
    public Button clearButton;
    public TextMeshProUGUI statusText;
    
    [Header("Skeleton Loader")]
    public SkeletonLoader skeletonLoader;
    
    void Start()
    {
        SetupUI();
        LoadSkeletonFileList();
    }
    
    void SetupUI()
    {
        if (skeletonLoader == null)
        {
            skeletonLoader = FindObjectOfType<SkeletonLoader>();
        }
        
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(LoadSelectedSkeleton);
        }
        
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearAllBoxes);
        }
        
        if (skeletonDropdown != null)
        {
            skeletonDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }
    
    void LoadSkeletonFileList()
    {
        if (skeletonDropdown == null) return;
        
        skeletonDropdown.ClearOptions();
        
        // Find all .skl.json files in the skeletons directory
        string skeletonsPath = "Assets/Data/skeletons/";
        if (System.IO.Directory.Exists(skeletonsPath))
        {
            string[] files = System.IO.Directory.GetFiles(skeletonsPath, "*.skl.json");
            System.Collections.Generic.List<string> fileNames = new System.Collections.Generic.List<string>();
            
            foreach (string file in files)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetFileNameWithoutExtension(file));
                fileNames.Add(fileName);
            }
            
            skeletonDropdown.AddOptions(fileNames);
            
            if (statusText != null)
            {
                statusText.text = $"Found {fileNames.Count} skeleton files";
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = "Skeletons directory not found!";
            }
        }
    }
    
    public void LoadSelectedSkeleton()
    {
        if (skeletonDropdown == null || skeletonDropdown.value < 0 || skeletonLoader == null) return;
        
        string selectedFile = skeletonDropdown.options[skeletonDropdown.value].text;
        
        if (statusText != null)
        {
            statusText.text = $"Loading {selectedFile}...";
        }
        
        // The actual loading will be handled by the SkeletonLoader component
        // This is just for UI feedback
        Invoke(nameof(UpdateStatusAfterLoad), 0.1f);
    }
    
    void UpdateStatusAfterLoad()
    {
        if (statusText != null && skeletonLoader != null)
        {
            int boxCount = skeletonLoader.transform.childCount;
            statusText.text = $"Loaded skeleton with {boxCount} PCJ boxes";
        }
    }
    
    public void ClearAllBoxes()
    {
        if (skeletonLoader != null)
        {
            skeletonLoader.ClearAllBoxes();
            
            if (statusText != null)
            {
                statusText.text = "Cleared all boxes";
            }
        }
    }
    
    void OnDropdownValueChanged(int value)
    {
        if (statusText != null && skeletonDropdown != null)
        {
            string selectedFile = skeletonDropdown.options[value].text;
            statusText.text = $"Selected: {selectedFile}";
        }
    }
}
