using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class GrapplingHook : MonoBehaviour
{
    public GameManager gameManager;

    public bool isGameRunning;

    public GameObject hook;
    public GameObject hookHolder;

    public float hookTravelSpeed;
    public float playerTravelSpeed;

    public bool fired;
    public bool hooked;
    public bool hookedToCar;
    public bool pulling;
    public GameObject hookedObj;
    GameObject oldHookedObj;

    public float maxDistance;
    float currentDistance;
    float distanceToHook;

    Car currentHookedCar;
    float currentHookedCarSpeed;

    public float checkRateForNoHooking = 1f;
    float nextCheckForNoHooking;

    public Rigidbody[] humanoidRigibodies;

    public GameObject hookCloseModelGO;
    public GameObject hookOpenModelGO;


    [Header("Slow Motion Effect & Game Over")]
    public bool isSlowMotionEnabled;
    public float slowMotionTimeScale = 0.3f;
    public float slowMotionEffectLength = 2f;
    public GameObject slowMotionVFXPanel;
    Coroutine slowMotionCoroutine;
    bool isTimerOn;
    float timerValue;
    public Text timerText;
    public int livesCount = 3;
    public Collider[] groundColliders;
    public GameObject gamePlayPanel;
    public GameObject gameOverPanel;
    public Transform livesLossUI;
    float ragdollExplostionForce = 100f;

    [Header("Score")]
    public int totalScore;
    public int score;
    public Text scoreText;
    Vector3 startingPosition;
    int tempScore;

    [Header("Animation")]
    public Animator wingsuitAnimator;

    [Space]
    public float leaningThreshold = 0.25f;
    public int allowedThresholdPercentage = 5;
    int lastThresholdPercentage;
    int thresholdPercentage;
    int currentThresholdPercentage;

    public float leaningTransitionDuration = 0.05f;
    float leaningTransitionTimelength;

    float lastPos;
    float currentPos;
    float differencePos;

    [Space]
    public float pullingThreshold = 100f;
    float pullingValue;

    public float pullingTransitionDuration = 0.05f;
    float PullingTransitionTimelength;

    [Header("Hook TimeLength & PostProcessing Effects")]
    public float postProcessingActivationThershold = 1.5f;
    float hookTimeLength = 3f;
    public float ppTransitionDuration = 2f;

    public Volume volume;
    LensDistortion lensDistortion;
    Bloom bloom;
    ChromaticAberration chromaticAberration;
    Vignette vignette;

    public float lensDistortionIntensity = 0.4f;
    public float bloomIntensity = 7f;
    public float chromaticAberrationIntensity = 0.5f;
    public float vignetteIntensity = 0.5f;

    ClampedFloatParameter lensDistortionIntensityFP;
    MinFloatParameter bloomIntensityFP;
    ClampedFloatParameter chromaticAberrationIntensityFP;
    ClampedFloatParameter vignetteIntensityFP;

    bool shouldEnableVignette;

    [Header("Highscore")]
    public float highscoreTimelengthThreshold = 1f;
    public int currentHighscore;
    public Text highscoreText;
    bool isHighscoreApplied;

    [Header("Scoring String Texts Popup")]
    public int scoreAddedValue = 20;
    public int distanceNeededFactor = 10;
    public Text scoringStringText;
    bool isDistaceReaded;
    int additionalScore;

    [Header("UI Animation")]
    public Animator scoreTextAnimator;
    public Animator highscoreTextAnimator;
    public Animator scoringStringTextAnimator;

    [Header("Cancel Game If Not Started")]
    bool shouldCancelGame = true;
    public float cancelGameCountdown = 4f;
    

    Transform myTransform;
    Rigidbody myRigidbody;
    SpringJoint mySpringJoint;

    LineRenderer rope;
    Camera mainCamera;

    Vector3 mousePosition;

    Ray camRay;
    RaycastHit rayHit;


    void Awake()
    {
        myTransform = transform;
        myRigidbody = GetComponent<Rigidbody>();
        mySpringJoint = GetComponent<SpringJoint>();
        rope = hook.GetComponent<LineRenderer>();

        mainCamera = Camera.main;

        //mousePosition.z = Vector3.Distance(new Vector3(0f, myTransform.position.y, 0f), new Vector3(0f, mainCamera.transform.position.y, 0f));

        startingPosition = myTransform.position;

        SetPPParameters();
        ResetAllPPParameters();
    }

    void Update()
    {
        //myTransform.Translate(Vector3.forward * Time.deltaTime * 15f);

        if (shouldCancelGame)
            CancelGameIfNotStarted();

        if (!gameManager.isLevelCompleted)
            CheckIfGameStarted();

        if (isGameRunning)
        {
            shouldCancelGame = false;

            CheckIfHookedToCar();

            StabilizePlayerWhenHooked();

            CalculateScore();

            PlayWingsuitAnimation();
        }


        if (Input.GetMouseButtonDown(0) && !fired)
        {
            //2D Gameview.
            //mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, mousePosition.z);
            //hookHolder.transform.LookAt(mainCamera.ScreenToWorldPoint(mousePosition));

            //3D Gameview.
            mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.nearClipPlane);

            camRay = mainCamera.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(camRay, out rayHit, 70f))
                hookHolder.transform.LookAt(rayHit.point);


            myRigidbody.isKinematic = true;
            mySpringJoint.connectedBody = null;

            ReturnHook();

            fired = true;
        }

        if (fired || hooked)
        {
            if (rope != null)
            {
                rope.positionCount = 2;
                rope.SetPosition(0, hookHolder.transform.position);
                rope.SetPosition(1, hook.transform.position);
            }
        }

        if (fired && !hooked)
        {
            hook.transform.Translate(Vector3.forward * Time.deltaTime * hookTravelSpeed);

            currentDistance = Vector3.Distance(myTransform.position, hook.transform.position);

            if (currentDistance >= maxDistance)
            {
                hookHolder.transform.localEulerAngles = Vector3.zero;
                ReturnHook();
            }
        }

        if (hooked && fired)
        {
            pulling = true;

            if (isSlowMotionEnabled)
                ResetAfterSlowMotion();

            hook.transform.parent = hookedObj.transform;

            myTransform.position = Vector3.MoveTowards(myTransform.position, hook.transform.position, Time.deltaTime * playerTravelSpeed);

            distanceToHook = Vector3.Distance(myTransform.position, hook.transform.position);

            if (distanceToHook < 1)
            {
                PlayTriggeredAnimation("Ready");
                HookingToCar();
            }
        }

        if (!fired && !hooked)
        {
            CheckIfNotHookedForCertainTime();

            hook.transform.parent = hookHolder.transform;

            if (hookedObj != null)
            {
                hookedObj.GetComponent<BoxCollider>().enabled = true;
                hookedObj = null;
            }
            if (oldHookedObj != null)
            {
                oldHookedObj.GetComponent<BoxCollider>().enabled = true;
                oldHookedObj = null;
            }
        }

        if (hookedObj != null)
            hookedObj.GetComponent<BoxCollider>().enabled = false;

        if (oldHookedObj != null)
            oldHookedObj.GetComponent<BoxCollider>().enabled = true;

        if (isGameRunning)
        {
            GetHookTimelength();

            if (ShouldActivatePostProcessingEffects())
                ActivatePostProcessingEffects();
            else
                DeactivatePostProcessingEffects();
        }

        ApplyHighscoreIfPossible();

        PopupScoringStringTexts();

        TurnTimerOn();

        if (shouldEnableVignette)
            EnableVignetteEffect();

        scoreTextAnimator.SetBool("AnimScoreText", pulling);
    }

    void CheckIfGameStarted()
    {
        if (!isGameRunning)
        {
            if (hooked && fired)
                isGameRunning = true;
        }
    }

    void CheckIfHookedToCar()
    {
        if(currentHookedCar != null)
        {
            hookedToCar = true;
        }
        else
        {
            hookedToCar = false;
        }
    }

    public void ReturnHook()
    {
        OpenHookLimbs(false);

        hook.transform.rotation = hookHolder.transform.rotation;
        hook.transform.position = hookHolder.transform.position;
        hook.transform.localScale = Vector3.one;

        fired = false;
        hooked = false;

        if (rope != null)
            rope.positionCount = 0;

        currentHookedCar = null;
    }

    void HookingToCar()
    {
        OpenHookLimbs(true);

        hook.transform.localPosition = new Vector3(0f, 1.1f, -0.5f);
        hook.transform.localEulerAngles = Vector3.zero;

        mySpringJoint.connectedBody = hook.GetComponent<Rigidbody>();
        myRigidbody.isKinematic = false;

        fired = false;
        pulling = false;

        currentHookedCar = hookedObj.GetComponent<Car>();
        currentHookedCar.IncreaseCarSpeedWhenPlayerConnected();

        if (oldHookedObj != null)
            oldHookedObj.GetComponent<Car>().ResetActualMovingSpeed();
    }

    public void SetHookedObj(GameObject gameObject)
    {
        if (hookedObj != null)
            oldHookedObj = hookedObj;

        hookedObj = gameObject;
    }

    void StabilizePlayerWhenHooked()
    {
        if (hookedToCar)
        {
            if (currentHookedCar.actualMovingSpeed != currentHookedCarSpeed)
            {
                currentHookedCarSpeed = currentHookedCar.actualMovingSpeed;
            }
        }
        else
        {
            currentHookedCarSpeed = 0f;
        }

        myTransform.Translate(Vector3.forward * Time.deltaTime * currentHookedCarSpeed);
    }

    public void DetachHookFromCar(Transform carToBeDestroyed) //Called From Car.
    {
        if (hookedToCar)
            if (carToBeDestroyed != null && currentHookedCar != null)
                if (carToBeDestroyed == currentHookedCar.transform)
                    ReturnHook();
    }

    IEnumerator RunSlowMotion()
    {
        livesCount--;
        livesLossUI.GetChild(3 - (livesCount + 1)).GetComponent<Image>().color = Color.red;

        isSlowMotionEnabled = true;
        slowMotionVFXPanel.SetActive(true);
        Time.timeScale = slowMotionTimeScale;

        timerValue = slowMotionEffectLength;
        isTimerOn = true;

        yield return new WaitForSeconds((slowMotionEffectLength * slowMotionTimeScale));

        Time.timeScale = 1f;
        slowMotionVFXPanel.SetActive(false);
        isSlowMotionEnabled = false;

        GameOver();
    }

    void TurnTimerOn()
    {
        if (isTimerOn)
        {
            if (!timerText.enabled)
                timerText.enabled = true;

            timerValue -= Time.unscaledDeltaTime;

            if (timerValue >= 0)
                timerText.text = string.Format("{0:00.00}", timerValue);
        }
        else
        {
            if (timerText.enabled)
                timerText.enabled = false;
        }
    }

    void ResetAfterSlowMotion()
    {
        StopCoroutine(slowMotionCoroutine);

        isTimerOn = false;
        isSlowMotionEnabled = false;
        Time.timeScale = 1f;
        slowMotionVFXPanel.SetActive(false);
    }

    void CheckIfNotHookedForCertainTime()
    {
        if (isGameRunning)
        {
            if (Time.time > nextCheckForNoHooking + Time.unscaledDeltaTime)
            {
                nextCheckForNoHooking = Time.time + checkRateForNoHooking;

                if (!hooked && !fired)
                {
                    if (livesCount <= 0)
                    {
                        GameOver();
                        return;
                    }

                    slowMotionCoroutine = StartCoroutine(RunSlowMotion());
                }
            }
        }
    }

    void EnableRagdoll()
    {
        foreach (Rigidbody rb in humanoidRigibodies)
        {
            rb.useGravity = true;
            rb.isKinematic = false;

            rb.AddExplosionForce(ragdollExplostionForce, (myTransform.position - Vector3.forward), 5f, 0f, ForceMode.Impulse);
        }
    }

    void OpenHookLimbs(bool state)
    {
        hookOpenModelGO.SetActive(state);
        hookCloseModelGO.SetActive(!state);
    }

    void GameOver()
    {
        Time.timeScale = 1f;

        foreach (Collider collider in groundColliders)
            collider.isTrigger = false;

        isGameRunning = false;
        shouldEnableVignette = true;
        gamePlayPanel.SetActive(false);

        hookHolder.SetActive(false);
        GetComponent<Collider>().enabled = false;
        wingsuitAnimator.enabled = false;

        EnableRagdoll();

        gameManager.SaveBestScore();

        gameOverPanel.SetActive(true);

        Invoke("DisableThisScript", ppTransitionDuration + 0.5f);
    }

    void DisableThisScript() //Called in GameOver.
    {
        this.enabled = false;
    }

    void EnableVignetteEffect()
    {
        if (vignetteIntensityFP.value != vignetteIntensity)
            vignetteIntensityFP.value = LerpFloatValue(vignetteIntensityFP.value, vignetteIntensity, 0f);

        vignette.intensity = vignetteIntensityFP;
    }

    void CalculateScore()
    {
        tempScore = (int)(myTransform.position.z - startingPosition.z);

        if (tempScore >= score)
            score = tempScore;

        totalScore = score + additionalScore;

        if (totalScore >= 0)
            scoreText.text = totalScore.ToString();
    }

    void PlayWingsuitAnimation()
    {
        //Leaning Part.
        currentPos = myTransform.position.x;
        differencePos = (currentPos - lastPos);
        lastPos = currentPos;

        currentThresholdPercentage = (int)((differencePos / leaningThreshold) * 100);

        if (currentThresholdPercentage < (lastThresholdPercentage + allowedThresholdPercentage)
            && currentThresholdPercentage > (lastThresholdPercentage - allowedThresholdPercentage))
        {
            leaningTransitionTimelength += Time.deltaTime / leaningTransitionDuration;
            thresholdPercentage = (int)Mathf.Lerp(thresholdPercentage, currentThresholdPercentage, leaningTransitionTimelength);
        }
        else
        {
            leaningTransitionTimelength = 0f;
        }

        lastThresholdPercentage = currentThresholdPercentage;

        wingsuitAnimator.SetFloat("LeaningValue", thresholdPercentage);

        //Moving Forward Part.
        if (pulling)
        {
            PullingTransitionTimelength += Time.deltaTime / pullingTransitionDuration;
            pullingValue = (int)Mathf.Lerp(pullingValue, pullingThreshold, PullingTransitionTimelength);
        }
        else
        {
            PullingTransitionTimelength = 0f;
            pullingValue = 0f;
        }

        wingsuitAnimator.SetFloat("PullingForwardValue", pullingValue);
    }

    void PlayTriggeredAnimation(string triggerName)
    {
        wingsuitAnimator.SetTrigger(triggerName);
    }

    void GetHookTimelength()
    {
        if (hooked)
        {
            hookTimeLength += Time.unscaledDeltaTime;
        }
        else
        {
            hookTimeLength = 0f;
        }
    }

    bool ShouldActivatePostProcessingEffects()
    {
        if (hookTimeLength < postProcessingActivationThershold && !isSlowMotionEnabled)        
            return true;
        else
            return false;
    }

    void SetPPParameters()
    {
        if (volume.profile.TryGet<LensDistortion>(out LensDistortion tmpLD))
        {
            lensDistortion = tmpLD;

            lensDistortionIntensityFP = lensDistortion.intensity;
        }

        if (volume.profile.TryGet<Bloom>(out Bloom tmpB))
        {
            bloom = tmpB;

            bloomIntensityFP = bloom.intensity;
        }

        if (volume.profile.TryGet<ChromaticAberration>(out ChromaticAberration tmpCA))
        {
            chromaticAberration = tmpCA;

            chromaticAberrationIntensityFP = chromaticAberration.intensity;
        }

        if (volume.profile.TryGet<Vignette>(out Vignette tmpV))
        {
            vignette = tmpV;

            vignetteIntensityFP = vignette.intensity;
        }
    }

    void ActivatePostProcessingEffects()
    {
        if (lensDistortionIntensityFP.value != lensDistortionIntensity)
            lensDistortionIntensityFP.value = LerpFloatValue(lensDistortionIntensityFP.value, lensDistortionIntensity, 0f);

        if (bloomIntensityFP.value != bloomIntensity)
            bloomIntensityFP.value = LerpFloatValue(bloomIntensityFP.value, bloomIntensity, 0f);

        if (chromaticAberrationIntensityFP.value != chromaticAberrationIntensity)
            chromaticAberrationIntensityFP.value = LerpFloatValue(chromaticAberrationIntensityFP.value, chromaticAberrationIntensity, 0f);

        ApplyPPParameters();
    }

    void DeactivatePostProcessingEffects()
    {
        if (lensDistortionIntensityFP.value != 0f)
            lensDistortionIntensityFP.value = LerpFloatValue(lensDistortionIntensityFP.value, 0f, 0f);

        if (bloomIntensityFP.value != 0f)
            bloomIntensityFP.value = LerpFloatValue(bloomIntensityFP.value, 0f, 0f);

        if (chromaticAberrationIntensityFP.value != 0f)
            chromaticAberrationIntensityFP.value = LerpFloatValue(chromaticAberrationIntensityFP.value, 0f, 0f);

        ApplyPPParameters();
    }

    void ApplyPPParameters()
    {
        lensDistortion.intensity = lensDistortionIntensityFP;
        bloom.intensity = bloomIntensityFP;
        chromaticAberration.intensity = chromaticAberrationIntensityFP;
    }

    void ResetAllPPParameters()
    {
        lensDistortionIntensityFP.value = 0f;
        bloomIntensityFP.value = 0f;
        chromaticAberrationIntensityFP.value = 0f;
        vignetteIntensityFP.value = 0f;

        lensDistortion.intensity = lensDistortionIntensityFP;
        bloom.intensity = bloomIntensityFP;
        chromaticAberration.intensity = chromaticAberrationIntensityFP;
    }

    float LerpFloatValue(float fromFloatValue, float toFloatValue, float ppTransitionTimelength)
    {
        ppTransitionTimelength += Time.unscaledDeltaTime / ppTransitionDuration;
        return Mathf.Lerp(fromFloatValue, toFloatValue, ppTransitionTimelength);
    }

    void ApplyHighscoreIfPossible()
    {
        if (hookTimeLength < highscoreTimelengthThreshold)
        {
            if (fired && !isHighscoreApplied)
            {
                isHighscoreApplied = true;

                currentHighscore += 2;

                highscoreTextAnimator.SetTrigger("AnimHighscore");

                if (Time.timeScale < 2)
                    Time.timeScale += 0.1f;
            }
        }
        else
        {
            currentHighscore = -2;
            Time.timeScale = 1f;
        }

        if (!fired)
            isHighscoreApplied = false;

        if (isSlowMotionEnabled)
            currentHighscore = -2;

        if (currentHighscore >= 2)
            highscoreText.text = currentHighscore + "X";
        else
            highscoreText.text = "";
    }

    void PopupScoringStringTexts()
    {
        if (!hooked && isDistaceReaded)
        {
            isDistaceReaded = false;
            scoringStringText.text = "";
        }

        if(isSlowMotionEnabled)
            scoringStringText.text = "";

        if (hooked && !isDistaceReaded && !isSlowMotionEnabled)
        {
            isDistaceReaded = true;

            if (currentDistance >= 1 && currentDistance < distanceNeededFactor)
                scoringStringText.text = "OK" + "\n" + "+" + ((int)currentDistance + scoreAddedValue);
            else if (currentDistance >= distanceNeededFactor && currentDistance < (distanceNeededFactor * 2))
                scoringStringText.text = "GOOD" + "\n" + "+" + ((int)currentDistance + scoreAddedValue);
            else if (currentDistance >= (distanceNeededFactor * 2) && currentDistance < (distanceNeededFactor * 3))
                scoringStringText.text = "GREAT" + "\n" + "+" + ((int)currentDistance + scoreAddedValue);
            else if (currentDistance >= (distanceNeededFactor * 3) && currentDistance < (distanceNeededFactor * 4))
                scoringStringText.text = "PERFECT" + "\n" + "+" + ((int)currentDistance + scoreAddedValue);

            scoringStringTextAnimator.SetTrigger("AnimScoringStringText");

            additionalScore += ((int)currentDistance + scoreAddedValue) * Mathf.Clamp(currentHighscore, 0, 100);
        }
    }

    void CancelGameIfNotStarted()
    {
        cancelGameCountdown -= Time.deltaTime;


        if (cancelGameCountdown <= 0)
        {
            ragdollExplostionForce = 4f;
            GameOver();
        }
    }
}