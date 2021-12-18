using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public CameraFollow cameraFollow;

    [Header("Play A Game")]
    public bool isGameStarted;
    public GrapplingHook grapplingHook;
    public GameObject[] disabledGameobjects;

    [Header("Best Score")]
    int bestScore;
    public Text bestScoreText;

    [Header("Levels")]
    public bool isLevelCompleted;
    public int currentLevelNumber;

    public int minRequiredScore;
    public int basicRequiredScore = 500;
    public int addedUpScoreEachLevel = 100;

    public GameObject finishLineBanner;

    public Image levelBar;
    public Image nextLevelImage;
    public Text nextLevelNumberText;
    public Text[] currentLevelNumberTexts;
    public GameObject levelCompletedPanel;
    public GameObject gameplayPanel;
    public Text levelCompletedScoreText;
    public Text levelCompletedText;

    [Header("Road Properties")]
    public Material sideRoadMaterial;
    public Texture[] sideRoadTextures;

    bool shouldResetRoadTextureOffset;
    public float roadAnimSpeed = 0.25f;
    public Material[] mainSideRoadMaterials;
    public Material roadCurbMaterial;

    [Header("Skins")]
    public int skinIndex;
    public Material skinMaterial;
    public Texture[] skinTextures;

    //UI Stuff
    int savedSkinIndex;
    public Color markedPreviewButtonColor;
    public Button skinSelectionButton;
    public Image[] skinPreviewButtonsImages;

    void Awake()
    {
        Application.targetFrameRate = 120;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        LoadBestScore();
        LoadLevelsData();

        UpdateLevelsUI();

        RandomizeRoadTexture();

        LoadSelectedSkin();
    }

    void Start()
    {
        InstantiateFinishLineBanner();
    }

    void Update()
    {
        if (!isGameStarted)
        {
            AnimateRoadTexture();

            return;
        }
        else
            shouldResetRoadTextureOffset = true;

        if (shouldResetRoadTextureOffset)
            ResetRoadTextureOffset();

        CheckIfLevelCompleted();
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(0);
        CarPool.poolCars.Clear();
        Time.timeScale = 1f;
    }

    public void PlayTheGame()
    {
        if (!isGameStarted)
        {
            isGameStarted = true;
            foreach (GameObject go in disabledGameobjects)
                go.SetActive(true);

            grapplingHook.enabled = true;
        }
    }

    public void SaveBestScore()
    {
        if (grapplingHook.totalScore > bestScore)
        {
            bestScore = grapplingHook.totalScore;
            bestScoreText.text = bestScore.ToString();

            DataSaveManager.SaveData("BestScore", bestScore);
        }
    }

    void LoadBestScore()
    {
        if (DataSaveManager.IsDataExist("BestScore"))
            bestScore = (int)DataSaveManager.LoadData("BestScore");

        bestScoreText.text = bestScore.ToString();
    }

    public void PauseGame()
    {
        grapplingHook.isGameRunning = false;
        grapplingHook.ReturnHook();
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;

        grapplingHook.ReturnHook();
    }

    void CheckIfLevelCompleted()
    {
        if (isLevelCompleted)
            return;

        levelBar.fillAmount = ((float)grapplingHook.score / (float)minRequiredScore);

        if (grapplingHook.score >= minRequiredScore)
        {
            nextLevelImage.color = Color.green;

            LevelCompleted();
            currentLevelNumber++;
            SaveLevelsData();

            isLevelCompleted = true;
        }
    }

    void LevelCompleted()
    {
        SaveBestScore();

        levelCompletedText.text = "LEVEL " + currentLevelNumber + " COMPLETED";

        levelCompletedScoreText.text = grapplingHook.totalScore.ToString();

        levelCompletedPanel.SetActive(true);
        gameplayPanel.SetActive(false);

        cameraFollow.enabled = false;
        grapplingHook.isGameRunning = false;
    }

    void UpdateLevelsUI()
    {
        foreach (Text _text in currentLevelNumberTexts)
            _text.text = currentLevelNumber.ToString();

        nextLevelNumberText.text = (currentLevelNumber + 1).ToString();
    }

    void SaveLevelsData()
    {
        DataSaveManager.SaveData("LevelNumber", currentLevelNumber);
    }

    void LoadLevelsData()
    {
        if (DataSaveManager.IsDataExist("LevelNumber"))
            currentLevelNumber = (int)DataSaveManager.LoadData("LevelNumber");

        minRequiredScore = (basicRequiredScore + (currentLevelNumber * addedUpScoreEachLevel));
    }

    void InstantiateFinishLineBanner()
    {
        Transform bannerTransform = Instantiate(finishLineBanner).transform;

        bannerTransform.position = new Vector3(0f, 0f, (minRequiredScore + grapplingHook.transform.position.z));

        //if(!cameraFollow.is3DView)
    }

    void RandomizeRoadTexture()
    {
        sideRoadMaterial.SetTexture("_BaseMap", sideRoadTextures[Random.Range(0, sideRoadTextures.Length)]);
    }

    void AnimateRoadTexture()
    {
        mainSideRoadMaterials[0].SetTextureOffset("_BaseMap", Vector2.up * Time.time * roadAnimSpeed);
        mainSideRoadMaterials[1].SetTextureOffset("_BaseMap", Vector2.up * Time.time * roadAnimSpeed * 0.25f);

        roadCurbMaterial.SetTextureOffset("_BaseMap", Vector2.right * Time.time * roadAnimSpeed);
    }

    void ResetRoadTextureOffset()
    {
        foreach (Material roadMat in mainSideRoadMaterials)
            roadMat.SetTextureOffset("_BaseMap", Vector2.zero);

        roadCurbMaterial.SetTextureOffset("_BaseMap", Vector2.zero);

        shouldResetRoadTextureOffset = false;
    }

    public void PreviewSkin(int previewSkinIndex)
    {
        skinIndex = previewSkinIndex;

        skinMaterial.SetTexture("_BaseMap", skinTextures[skinIndex]);

        UpdateSkinsMenuUI();
    }

    public void SelectSkin()
    {
        SaveSelectedSkin();

        UpdateSkinsMenuUI();
    }

    void SaveSelectedSkin()
    {
        DataSaveManager.SaveData("SkinIndex", skinIndex);
    }

    public void LoadSelectedSkin()
    {
        if (DataSaveManager.IsDataExist("SkinIndex"))
            skinIndex = (int)DataSaveManager.LoadData("SkinIndex");
        else
            skinIndex = 0;

        skinMaterial.SetTexture("_BaseMap", skinTextures[skinIndex]);
    }

    public void UpdateSkinsMenuUI()
    {
        foreach (Image image in skinPreviewButtonsImages)
            image.color = Color.white;

        skinPreviewButtonsImages[skinIndex].color = markedPreviewButtonColor;

        if (DataSaveManager.IsDataExist("SkinIndex"))
            savedSkinIndex = (int)DataSaveManager.LoadData("SkinIndex");

        if (skinIndex == savedSkinIndex)
        {
            skinSelectionButton.interactable = false;
            skinSelectionButton.GetComponent<Image>().color = Color.gray;
            skinSelectionButton.GetComponentInChildren<Text>().text = "SELECTED";
        }
        else
        {
            skinSelectionButton.interactable = true;
            skinSelectionButton.GetComponent<Image>().color = Color.green;
            skinSelectionButton.GetComponentInChildren<Text>().text = "SELECT";
        }

    }

    public void TinySauceGameStarted()
    {
        TinySauce.OnGameStarted(levelNumber: currentLevelNumber.ToString());
    }

    public void TinySauceGameFinished()
    {
        //print(currentLevelNumber.ToString() + "     false     " + grapplingHook.totalScore);
        TinySauce.OnGameFinished(levelNumber: currentLevelNumber.ToString(), false, grapplingHook.totalScore);
    }

    public void TinySauceLevelCompleted()
    {
        //print((currentLevelNumber - 1).ToString() + "     true    " + grapplingHook.totalScore);
        TinySauce.OnGameFinished(levelNumber: (currentLevelNumber - 1).ToString(), true, grapplingHook.totalScore);
    }
}
