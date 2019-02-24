using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class StandardizedBowForHand : MonoBehaviour
{
    #region Private Values
    // PRIVATE VALUES
    private float currentTime, lerpPercentage, startingAngleDown, startingAngleUp, currentAngleDown, currentAngleUp;
    private Vector3 stringStartPos, stringEndPos, stringLastPos, bowDirPull, bowDirRetract;
    private Vector3 firstStartUpJointRot1, firstStartUpJointRot2, firstStartUpJointRot3, firstStartDownJointRot1, firstStartDownJointRot2, firstStartDownJointRot3;
    private Vector3 firstEndUpJointRot1, firstEndUpJointRot2, firstEndUpJointRot3, firstEndDownJointRot1, firstEndDownJointRot2, firstEndDownJointRot3;
    private Vector3 firstLastUpJointRot1, firstLastUpJointRot2, firstLastUpJointRot3, firstLastDownJointRot1, firstLastDownJointRot2, firstLastDownJointRot3;
    private GameObject lastProjectile, stringStartObj, stringEndObj, poolHolder;
    private Transform stringEndObjTrans, stringStartObjTrans, poolHolderTrans;
    private StandardizedProjectileForHand lastProjectileScript;
    [HideInInspector]
    public Queue<GameObject> projectilePool; // Object Pooling Queue
    private Transform lastProjectileTransform;
    private Rigidbody lastProjectileRigidbody;
    private AudioSource audioSource;
    private float currentStressOnString = 0f;
    // ANIM PRIVATES
    private int pullStateHash, pullStateHash2;
    private AnimatorStateInfo currentStateInfo;
    private float projectileDelayTimer, pullDelayTimer;
    // INSTANT STATE BOOLS
    private bool justLeftString = false, justPulledString = true, justStoppedPulling=true;
    // CONSTANTS
    private const float PI = Mathf.PI;
    private const float HALFPI = Mathf.PI / 2.0f;
    #endregion

    #region Public Values
    // PUBLIC VALUES
    [Header("     Bow User Related")]
    [Tooltip("Animator component of the bow user.")]
    public Animator bowUserAnim;
    [Tooltip("The transform that the string should stick to. Usually it is the other hand of the bow user.")]
    public Transform bowUserPullHand;
    [Tooltip("Where are the animation states that you defined below are in bowUserAnim.")]
    public int pullStateLayerIndex = 0;
    [Tooltip("Name of the state that triggers shooting process.")]
    public string pullStateName = "Drawing";
    [Tooltip("Same thing with normal pullStateName. It is not mandatory. Just place a state like overdrawing if you have.")]
    public string pullStateName2 = "Hold";
    [Tooltip("Amount of time delay to spawn the projectile.")]
    public float pullStateDelay=0.4f;
    [Tooltip("Amount of time delay to stick string to the bowUserPullHand.")]
    public float projectileHoldDelay=0.2f;
    [Tooltip("If it is true projectile spawns in bow user pull hand, if false it spawns on string.")]
    public bool projectileOnHand = true;
    //**************************
    [Header("     Bow Skeleton Parts")]
    [Tooltip("Upper bow transform. It must be right above the center position.")]
    public Transform bowUpJoint1;
    [Tooltip("Upper bow transform. It must be above the bowUpJoint1 position.")]
    public Transform bowUpJoint2;
    [Tooltip("Upper bow transform. It must be above the bowUpJoint2 position.")]
    public Transform bowUpJoint3;
    [Tooltip("Lower bow transform. It must be right below the center position.")]
    public Transform bowDownJoint1;
    [Tooltip("Second Lower bow transform. It must be below the bowDownJoint1 position.")]
    public Transform bowDownJoint2;
    [Tooltip("Second Lower bow transform. It must be below the bowDownJoint2 position.")]
    public Transform bowDownJoint3;
    [Tooltip("The transform of the string. It MUST be rigged; meaning that it is in the center and deforms the string as a V shape when dragged.")]
    public Transform bowStringPoint;
    //***************************
    public enum jointsDirection { XAxis, YAxis, ZAxis }
    [Header("     Bow Joints Related")]
    [Tooltip("Direction of rotation for joints. Set your Unity selection to Pivot/Local and look at the rotation chamber of the joint in armature. Red is X, Blue is Z, Green is Y.")]
    public jointsDirection joint1RotateDirectionAxis = jointsDirection.ZAxis;
    [Tooltip("Direction of rotation for joints. Set your Unity selection to Pivot/Local and look at the rotation chamber of the joint in armature. Red is X, Blue is Z, Green is Y.")]
    public jointsDirection joint2RotateDirectionAxis = jointsDirection.XAxis;
    [Tooltip("Direction of rotation for joints. Set your Unity selection to Pivot/Local and look at the rotation chamber of the joint in armature. Red is X, Blue is Z, Green is Y.")]
    public jointsDirection joint3RotateDirectionAxis = jointsDirection.XAxis;
    [Range(-45f, 45f), Tooltip("Angle limit for the first joints in euler angles.")]
    public float bendAngleLimitFirstJoints = 10f;
    [Range(-45f, 45f), Tooltip("Angle limit for the second joints in euler angles. Usually it should be lower than the first angle limit but try it out and find the best one that suits yours.")]
    public float bendAngleLimitSecondJoints = 10f;
    [Range(-45f, 45f), Tooltip("Angle limit for the second joints in euler angles. Usually it should be lower than the first angle limit but try it out and find the best one that suits yours.")]
    public float bendAngleLimitThirdJoints = 10f;
    [Tooltip("Inverses the bending direction of the joints.")]
    public bool inverseBend = false;
    //***************************
    public enum axisDirection { XAxis, YAxis, ZAxis }
    [Header("     Bow String Related")]
    public axisDirection stringMoveDirectionAxis = axisDirection.YAxis;
    [Range(0.2f, 0.8f), Tooltip("The maximum string retract(pull back) amount from its starting position to end position.")]
    public float stringMoveAmount = 0.5f;
    [Range(0.5f, 3f), Tooltip("The maximum string stress time amount from its starting position to end position. How much time is required to reach max string stress.")]
    public float stringMoveTime = 2f;
    [Range(0.1f, 1f), Tooltip("The maximum string retract time amount from its starting position to end position.")]
    public float stringRetractTime = 0.8f;
    [Tooltip("Inverses the pulling direction of the string.")]
    public bool inversePull = false;
    //***************************   
    public enum stringStressEaseType
    {
        VerySlowStart,
        SlowStart,
        MediumStable,
        FastStart,
        VeryFastStart,
        SpecialDraw1,
        SpecialDraw2,
        Linear
    }
    public enum stringRetractEaseType
    {
        Elastic,
        Bounce
    }
    [Header("     Bow Physics")]
    [Tooltip("Ease type when pulling the string.")]
    public stringStressEaseType stringMovementChoice = stringStressEaseType.FastStart;
    [Tooltip("Ease type when leaving the string.")]
    public stringRetractEaseType stringRetractChoice = stringRetractEaseType.Elastic;
    [Tooltip("Maximum stress built in string. It gets bigger as you hold it and it effects the speed of projectile. ")]
    public float maxStringStrength = 30f;
    public enum accuracyProjectile { PerfectAccuracy, SlightlyOff, DecentAccuracy, Unstable }
    [Tooltip("How much the projectile deters from where you aim.")]
    public accuracyProjectile projectileAccuracy = accuracyProjectile.PerfectAccuracy;
    //***************************  
    [Header("     Projectile Related")]
    [Tooltip("Gameobject that is going to be fired(Arrow).")]
    public GameObject projectile;
    [Tooltip("Object pooling size for projectiles in order to avoid Instantiating in runtime which is slower. Determine the pool size depending on your needs.")]
    public int projectilePoolSize = 15;
    [Tooltip("Offset of the projectile hold position.Leave 0 if it is on the correct position.")]
    public Vector3 projectileHoldPosOffset;
    [Tooltip("Local or world space offset for the projectile")]
    public bool isPosOffsetLocal = true;
    [Tooltip("Offset of the projectile hold euler angles.Leave 0 if it is on the correct rotation.")]
    public Vector3 projectileHoldRotOffset=new Vector3(90f,0,0);
    [Tooltip("Look at your projectile prefab. The axis that the tip of the arrow is looking is the projectileForwardAxis. THIS IS VERY IMPORTANT. If possible just make your projectile prefab to look blue arrow and set this to Z-Azis")]
    public axisDirection projectileForwardAxis = axisDirection.ZAxis;
    //***************************  
    [Header("     Bow Utilities")]
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;
    public AudioClip pullSound;
    public AudioClip retractSound;
    [Tooltip("Enable this bool if you want stress on string to effect the sound.")]
    public bool stressEffectOnSound = false;
    #endregion

    //*****************************************************************************************************************************

    // Start is called before the first frame update
    void Start()
    {
        switch (stringMoveDirectionAxis)
        {
            case axisDirection.XAxis:
                if (inversePull)
                {
                    bowDirPull = -bowStringPoint.right; bowDirRetract = bowStringPoint.right; break;
                }
                else
                {
                    bowDirPull = bowStringPoint.right; bowDirRetract = -bowStringPoint.right; break;
                }
            case axisDirection.YAxis:
                if (inversePull)
                {
                    bowDirPull = -bowStringPoint.up; bowDirRetract = bowStringPoint.up; break;
                }
                else
                {
                    bowDirPull = bowStringPoint.up; bowDirRetract = -bowStringPoint.up; break;
                }
            case axisDirection.ZAxis:
                if (inversePull)
                {
                    bowDirPull = -bowStringPoint.forward; bowDirRetract = bowStringPoint.forward; break;
                }
                else
                {
                    bowDirPull = bowStringPoint.forward; bowDirRetract = -bowStringPoint.forward; break;
                }
            default: break;
        }
        /* I AM TRYING TO DO THIS WITHOUT SPAWNING EMPTY GAMEOBJECTS. IT IS VERY EASY FOR A STABLE-NON MOVING AND ROTATING 
        BOW BUT ONCE THE BOW STARTS TO MOVE AND ROTATE, THE DRAW AND RETRACT MOTION IN THE STRING STARTS TO ACT WEIRD BECAUSE
        OF THE WAY I HANDLE EASING AND UPDATES. SPAWNING EMPTY OBJECTS IS NOT A BIG PROBLEM SINCE THEY ARE ONLY IN THE START
        FUNCTION BUT IT FORCES US TO CHANGE THEIR POSITION MANUALLY WHEN PLAYING WITH STRING MOVE AMOUNT IN RUNTIME.
        ----------------------------------------
        SINCE THIS IS A COMBINATION OF MODEL NORMALS AND UNITY ENVIRONMENT, IT IS HARD TO FIND ONE SOLUTION THAT IS GOING TO
        WORK FOR EVERYONES BOW MODEL. I WILL DEFINITELY TRY TO FIND A BETTER WAY TO DO THIS. BUT CURRENTLY, THIS IS THE BEST SOLUTION
        I CAN FIND FOR THIS PROBLEM*/
        stringStartPos = bowStringPoint.position;
        stringStartObj = new GameObject(transform.name + "StringStartPlaceHolder");
        stringStartObjTrans = stringStartObj.transform;
        stringStartObjTrans.position = stringStartPos;
        stringEndPos = stringStartPos + bowDirPull * stringMoveAmount;
        stringEndObj = new GameObject(transform.name + "StringEndPlaceHolder");
        stringEndObjTrans = stringEndObj.transform;
        stringEndObjTrans.position = stringEndPos;
        stringEndObjTrans.parent = transform;
        stringStartObjTrans.parent = transform;
        // Pool Object Holder - Quiver - Prevents Object Pooling to litter hierarchy
        poolHolder = new GameObject(transform.name + " Quiver");
        poolHolderTrans = poolHolder.transform;
        //
        firstStartUpJointRot1 = bowUpJoint1.localEulerAngles;
        firstStartUpJointRot2 = bowUpJoint2.localEulerAngles;
        firstStartUpJointRot3 = bowUpJoint3.localEulerAngles;
        firstStartDownJointRot1 = bowDownJoint1.localEulerAngles;
        firstStartDownJointRot2 = bowDownJoint2.localEulerAngles;
        firstStartDownJointRot3 = bowDownJoint3.localEulerAngles;
        //-------------------------------------------
        switch (joint1RotateDirectionAxis)
        {
            case jointsDirection.XAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot1 = firstStartUpJointRot1 - Vector3.right * bendAngleLimitFirstJoints;
                    firstEndDownJointRot1 = firstStartDownJointRot1 + Vector3.right * bendAngleLimitFirstJoints;
                }
                else
                {
                    firstEndUpJointRot1 = firstStartUpJointRot1 + Vector3.right * bendAngleLimitFirstJoints;
                    firstEndDownJointRot1 = firstStartDownJointRot1 - Vector3.right * bendAngleLimitFirstJoints;
                }
                break;
            case jointsDirection.YAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot1 = firstStartUpJointRot1 - Vector3.up * bendAngleLimitFirstJoints;
                    firstEndDownJointRot1 = firstStartDownJointRot1 + Vector3.up * bendAngleLimitFirstJoints;
                }
                else
                {
                    firstEndUpJointRot1 = firstStartUpJointRot1 + Vector3.up * bendAngleLimitFirstJoints;
                    firstEndDownJointRot1 = firstStartDownJointRot1 - Vector3.up * bendAngleLimitFirstJoints;
                }
                break;
            case jointsDirection.ZAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot1 = firstStartUpJointRot1 - Vector3.forward * bendAngleLimitFirstJoints;
                    firstEndDownJointRot1 = firstStartDownJointRot1 + Vector3.forward * bendAngleLimitFirstJoints;
                }
                else
                {
                    firstEndUpJointRot1 = firstStartUpJointRot1 + Vector3.forward * bendAngleLimitFirstJoints;
                    firstEndDownJointRot1 = firstStartDownJointRot1 - Vector3.forward * bendAngleLimitFirstJoints;
                }
                break;
            default:
                break;
        }
        switch (joint2RotateDirectionAxis)
        {
            case jointsDirection.XAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot2 = firstStartUpJointRot2 + Vector3.right * bendAngleLimitSecondJoints;
                    firstEndDownJointRot2 = firstStartDownJointRot2 - Vector3.right * bendAngleLimitSecondJoints;
                }
                else
                {
                    firstEndUpJointRot2 = firstStartUpJointRot2 - Vector3.right * bendAngleLimitSecondJoints;
                    firstEndDownJointRot2 = firstStartDownJointRot2 + Vector3.right * bendAngleLimitSecondJoints;
                }
                break;
            case jointsDirection.YAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot2 = firstStartUpJointRot2 + Vector3.up * bendAngleLimitSecondJoints;
                    firstEndDownJointRot2 = firstStartDownJointRot2 - Vector3.up * bendAngleLimitSecondJoints;
                }
                else
                {
                    firstEndUpJointRot2 = firstStartUpJointRot2 - Vector3.up * bendAngleLimitSecondJoints;
                    firstEndDownJointRot2 = firstStartDownJointRot2 + Vector3.up * bendAngleLimitSecondJoints;
                }
                break;
            case jointsDirection.ZAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot2 = firstStartUpJointRot2 + Vector3.forward * bendAngleLimitSecondJoints;
                    firstEndDownJointRot2 = firstStartDownJointRot2 - Vector3.forward * bendAngleLimitSecondJoints;
                }
                else
                {
                    firstEndUpJointRot2 = firstStartUpJointRot2 - Vector3.forward * bendAngleLimitSecondJoints;
                    firstEndDownJointRot2 = firstStartDownJointRot2 + Vector3.forward * bendAngleLimitSecondJoints;
                }
                break;
            default:
                break;
        }
        switch (joint3RotateDirectionAxis)
        {
            case jointsDirection.XAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot3 = firstStartUpJointRot3 + Vector3.right * bendAngleLimitThirdJoints;
                    firstEndDownJointRot3 = firstStartDownJointRot3 - Vector3.right * bendAngleLimitThirdJoints;
                }
                else
                {
                    firstEndUpJointRot3 = firstStartUpJointRot3 - Vector3.right * bendAngleLimitThirdJoints;
                    firstEndDownJointRot3 = firstStartDownJointRot3 + Vector3.right * bendAngleLimitThirdJoints;
                }
                break;
            case jointsDirection.YAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot3 = firstStartUpJointRot3 + Vector3.up * bendAngleLimitThirdJoints;
                    firstEndDownJointRot3 = firstStartDownJointRot3 - Vector3.up * bendAngleLimitThirdJoints;
                }
                else
                {
                    firstEndUpJointRot3 = firstStartUpJointRot3 - Vector3.up * bendAngleLimitThirdJoints;
                    firstEndDownJointRot3 = firstStartDownJointRot3 + Vector3.up * bendAngleLimitThirdJoints;
                }
                break;
            case jointsDirection.ZAxis:
                if (inverseBend)
                {
                    firstEndUpJointRot3 = firstStartUpJointRot3 + Vector3.forward * bendAngleLimitThirdJoints;
                    firstEndDownJointRot3 = firstStartDownJointRot3 - Vector3.forward * bendAngleLimitThirdJoints;
                }
                else
                {
                    firstEndUpJointRot3 = firstStartUpJointRot3 - Vector3.forward * bendAngleLimitThirdJoints;
                    firstEndDownJointRot3 = firstStartDownJointRot3 + Vector3.forward * bendAngleLimitThirdJoints;
                }
                break;
            default:
                break;
        }
        //
        ProjectileObjectPool();
        // SOUND INIT 
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = soundVolume;
        audioSource.pitch = 1 + Mathf.Abs(2.5f - stringMoveTime);
        // ANIM INIT
        pullStateHash = Animator.StringToHash(pullStateName);
        pullStateHash2 = Animator.StringToHash(pullStateName2);
    }

    // Update is called once per frame
    void Update()
    {
        // Update current animator state
        currentStateInfo = bowUserAnim.GetCurrentAnimatorStateInfo(pullStateLayerIndex);
        // UPDATE STATE 1 - Pulling the string
        if (currentStateInfo.shortNameHash == pullStateHash2 || currentStateInfo.shortNameHash == pullStateHash)
        {
            // UPDATE STATE 2 - PROJECTILE SPAWN DELAY
            if (projectileDelayTimer>projectileHoldDelay)
            {
                if (justPulledString)
                {
                    currentTime = 0;
                    justPulledString = false;
                    justStoppedPulling = false;
                    ProjectileFollowString();
                    if (pullSound != null)
                    {
                        if (stressEffectOnSound)
                        {
                            audioSource.pitch = 1 + Mathf.Abs(2.5f - stringMoveTime);
                        }
                        audioSource.PlayOneShot(pullSound);
                    }
                }
                // UPDATE STATE 3 - STRING HOLD DELAY
                if (pullDelayTimer>pullStateDelay)
                {
                    currentTime += Time.deltaTime;
                    if (currentTime > stringMoveTime)
                    {
                        currentTime = stringMoveTime;
                    }
                    StringPull();
                    RotateDownJoints();
                    RotateUpperJoints();
                }
                else
                {
                    // Waiting for the bow user hand delay
                    pullDelayTimer += Time.deltaTime;
                }
            }
            else
            {
                // Waiting for projectile delay
                projectileDelayTimer += Time.deltaTime;
            } 
        }
        else
        {
            // UPDATE STATE 4 - Just released the string
            if (!justStoppedPulling)
            {
                currentTime = 0;
                stringLastPos = bowStringPoint.position;
                firstLastUpJointRot1 = bowUpJoint1.localEulerAngles;
                firstLastUpJointRot2 = bowUpJoint2.localEulerAngles;
                firstLastUpJointRot3 = bowUpJoint3.localEulerAngles;
                firstLastDownJointRot1 = bowDownJoint1.localEulerAngles;
                firstLastDownJointRot2 = bowDownJoint2.localEulerAngles;
                firstLastDownJointRot3 = bowDownJoint3.localEulerAngles;
                justPulledString = true;
                justLeftString = true;
                justStoppedPulling = true;
                ShootProjectile(currentStressOnString);
                if (stressEffectOnSound)
                {
                    audioSource.pitch = audioSource.pitch/2 + (currentStressOnString / maxStringStrength) * audioSource.pitch/2;
                }                
                currentStressOnString = 0;
                //
                audioSource.Stop();
                if (retractSound != null)
                {
                    audioSource.PlayOneShot(retractSound);
                }
            }
            // UPDATE STATE 5 - Released bow returning to stable form
            if (justLeftString)
            {
                currentTime += Time.deltaTime;
                if (currentTime > stringRetractTime)
                {
                    justLeftString = false;
                }
                RetractString();
                RotateBackJoints();
            }
            else
            {
                // UPDATE STATE 0 - Steady state, stable form
                currentTime = 0;
                currentStressOnString = 0;
                projectileDelayTimer = 0;
                pullDelayTimer = 0;
            }
        }

    }

    #region STRING RELATED
    // Fire string pull method
    void StringPull()
    {
        //
        lerpPercentage = currentTime / stringMoveTime;
        lerpPercentage = InterpolateStringStress(lerpPercentage);
        bowStringPoint.position = bowUserPullHand.position;
        //
        currentStressOnString = Mathf.Lerp(0, maxStringStrength, lerpPercentage);
    }
    // Ease the string pull
    float InterpolateStringStress(float lerpPerc)
    {
        switch (stringMovementChoice)
        {
            default: return lerpPerc;
            case stringStressEaseType.Linear: return lerpPerc;
            case stringStressEaseType.VerySlowStart: return CubicEaseIn(lerpPerc);
            case stringStressEaseType.SlowStart: return QuadraticEaseIn(lerpPerc);
            case stringStressEaseType.MediumStable: return SineEaseIn(lerpPerc);
            case stringStressEaseType.FastStart: return QuadraticEaseOut(lerpPerc);
            case stringStressEaseType.VeryFastStart: return CubicEaseOut(lerpPerc);
            case stringStressEaseType.SpecialDraw1: return BounceEaseOut(lerpPerc);
            case stringStressEaseType.SpecialDraw2: return BounceEaseIn(lerpPerc);
        }
    }
    // Fire string retract method
    void RetractString()
    {
        lerpPercentage = currentTime / stringRetractTime;
        lerpPercentage = InterpolateStringRetract(lerpPercentage);
        bowStringPoint.position = Vector3.LerpUnclamped(stringLastPos, stringStartObjTrans.position, lerpPercentage);
    }
    // Ease the string retract
    float InterpolateStringRetract(float lerpPerc)
    {
        switch (stringRetractChoice)
        {
            default: return lerpPerc;
            case stringRetractEaseType.Elastic: return ElasticEaseOut(lerpPerc);
            case stringRetractEaseType.Bounce: return BounceEaseOut(lerpPerc);
        }
    }
    #endregion

    #region JOINT RELATED
    // Down Joint Movements
    void RotateUpperJoints()
    {
        lerpPercentage = currentTime / stringMoveTime;
        lerpPercentage = InterpolateStringStress(lerpPercentage);
        bowUpJoint1.localEulerAngles = Vector3.Lerp(firstStartUpJointRot1, firstEndUpJointRot1, lerpPercentage);
        bowUpJoint2.localEulerAngles = Vector3.Lerp(firstStartUpJointRot2, firstEndUpJointRot2, lerpPercentage);
        bowUpJoint3.localEulerAngles = Vector3.Lerp(firstStartUpJointRot3, firstEndUpJointRot3, lerpPercentage);
    }
    // Upper Joint Movements
    void RotateDownJoints()
    {
        lerpPercentage = currentTime / stringMoveTime;
        lerpPercentage = InterpolateStringStress(lerpPercentage);
        bowDownJoint1.localEulerAngles = Vector3.Lerp(firstStartDownJointRot1, firstEndDownJointRot1, lerpPercentage);
        bowDownJoint2.localEulerAngles = Vector3.Lerp(firstStartDownJointRot2, firstEndDownJointRot2, lerpPercentage);
        bowDownJoint3.localEulerAngles = Vector3.Lerp(firstStartDownJointRot3, firstEndDownJointRot3, lerpPercentage);
    }
    // Return joints to the natural state
    void RotateBackJoints()
    {
        lerpPercentage = currentTime / stringRetractTime;
        lerpPercentage = InterpolateStringRetract(lerpPercentage);
        bowUpJoint1.localEulerAngles = Vector3.LerpUnclamped(firstLastUpJointRot1, firstStartUpJointRot1, lerpPercentage);
        bowUpJoint2.localEulerAngles = Vector3.LerpUnclamped(firstLastUpJointRot2, firstStartUpJointRot2, lerpPercentage);
        bowUpJoint3.localEulerAngles = Vector3.LerpUnclamped(firstLastUpJointRot3, firstStartUpJointRot3, lerpPercentage);
        bowDownJoint1.localEulerAngles = Vector3.LerpUnclamped(firstLastDownJointRot1, firstStartDownJointRot1, lerpPercentage);
        bowDownJoint2.localEulerAngles = Vector3.LerpUnclamped(firstLastDownJointRot2, firstStartDownJointRot2, lerpPercentage);
        bowDownJoint3.localEulerAngles = Vector3.LerpUnclamped(firstLastDownJointRot3, firstStartDownJointRot3, lerpPercentage);
    }
    #endregion

    #region SHOOTING RELATED
    void ProjectileFollowString()
    {
        // If the queue is empty
        if (projectilePool.Count == 0)
        {
            // Increase the pool size and add an extra projectile(I would suggest you to have a large pool size to avoid this. This is here for safety)
            AddExtraProjectileToPool();
            // Again the same procedure with normal dequeue with just avoiding GetComponent repetition
            lastProjectile = projectilePool.Dequeue();
            lastProjectileTransform = lastProjectile.transform;
            //lastProjectileRigidbody = lastProjectile.GetComponent<Rigidbody>();  // DONE INSIDE THE ADD METHOD
            if (projectileOnHand)
            {
                lastProjectileTransform.parent = bowUserPullHand;
                lastProjectileTransform.position = bowUserPullHand.position + projectileHoldPosOffset;
            }
            else
            {
                lastProjectileTransform.parent = bowStringPoint;
                lastProjectileTransform.position = bowStringPoint.position + projectileHoldPosOffset;
            }
            lastProjectileTransform.localEulerAngles = projectileHoldRotOffset;
            lastProjectile.SetActive(true);
        }
        else
        {
            // Just dequeue the first
            lastProjectile = projectilePool.Dequeue();
            lastProjectileTransform = lastProjectile.transform;
            lastProjectileRigidbody = lastProjectile.GetComponent<Rigidbody>();  // Try to avoid get component in runtime
            if (projectileOnHand)
            {
                lastProjectileTransform.parent = bowUserPullHand;
                if (isPosOffsetLocal)
                {
                    lastProjectileTransform.localPosition = bowStringPoint.localPosition + projectileHoldPosOffset; // Local Pos offset
                }
                else
                {
                    lastProjectileTransform.position = bowStringPoint.position + projectileHoldPosOffset; // World Pos offset
                }                
            }
            else
            {
                lastProjectileTransform.parent = bowStringPoint;
                if (isPosOffsetLocal)
                {
                    lastProjectileTransform.localPosition = bowStringPoint.localPosition + projectileHoldPosOffset; // Local Pos offset
                }
                else
                {
                    lastProjectileTransform.position = bowStringPoint.position + projectileHoldPosOffset; // World Pos offset
                }                
            }
            lastProjectileTransform.localEulerAngles = projectileHoldRotOffset;
            lastProjectile.SetActive(true);
        }
    }
    float x, y;
    void ShootProjectile(float currentPower)
    {        
        switch (projectileAccuracy)
        {
            case accuracyProjectile.PerfectAccuracy:
                x = 0;
                y = 0;
                break;
            case accuracyProjectile.SlightlyOff:
                x = Random.Range(-1f, 0.5f);
                y = Random.Range(-1f, 1f);
                break;
            case accuracyProjectile.DecentAccuracy:
                x = Random.Range(-2f, 1.5f);
                y = Random.Range(-2f, 2f);
                break;
            case accuracyProjectile.Unstable:
                x = Random.Range(-3f, 2f);
                y = Random.Range(-3f, 3f);
                break;
            default:
                Debug.Log("Projectile accuracy switch error.");
                break;
        }
        // Axis switch for different models
        switch (projectileForwardAxis)
        {
            case axisDirection.XAxis:
                lastProjectileTransform.localEulerAngles = lastProjectileTransform.localEulerAngles + Vector3.forward * x + Vector3.up * y;
                lastProjectileTransform.parent = null;
                lastProjectileRigidbody.useGravity = true;
                lastProjectileRigidbody.velocity = (lastProjectileTransform.right * currentPower);
                break;
            case axisDirection.YAxis:
                lastProjectileTransform.localEulerAngles = lastProjectileTransform.localEulerAngles + Vector3.right * x + Vector3.forward * y;
                lastProjectileTransform.parent = null;
                lastProjectileRigidbody.useGravity = true;
                lastProjectileRigidbody.velocity = (lastProjectileTransform.up * currentPower);
                break;
            case axisDirection.ZAxis:
                lastProjectileTransform.localEulerAngles = lastProjectileTransform.localEulerAngles + Vector3.up * x + Vector3.right * y;
                lastProjectileTransform.parent = null;
                lastProjectileRigidbody.useGravity = true;
                lastProjectileRigidbody.velocity = (lastProjectileTransform.forward * currentPower);
                break;
            default:
                Debug.Log("Error on projectile axis switch!");
                break;
        }        
    }
    #endregion

    #region EASING FUNCTIONS
    /// <summary>
    /// Modeled after the cubic y = x^3
    /// </summary>
    static public float CubicEaseIn(float p)
    {
        return p * p * p;
    }
    /// <summary>
    /// Modeled after the parabola y = x^2
    /// </summary>
    static public float QuadraticEaseIn(float p)
    {
        return p * p;
    }
    /// <summary>
    /// Modeled after quarter-cycle of sine wave
    /// </summary>
    static public float SineEaseIn(float p)
    {
        return Mathf.Sin((p - 1) * HALFPI) + 1;
    }
    /// <summary>
    /// Modeled after the parabola y = -x^2 + 2x
    /// </summary>
    static public float QuadraticEaseOut(float p)
    {
        return -(p * (p - 2));
    }
    /// <summary>
    /// Modeled after the cubic y = (x - 1)^3 + 1
    /// </summary>
    static public float CubicEaseOut(float p)
    {
        float f = (p - 1);
        return f * f * f + 1;
    }
    /// <summary>	
    /// Modeled after the piecewise circular function
    /// y = (1/2)(1 - Math.Sqrt(1 - 4x^2))           ; [0, 0.5)
    /// y = (1/2)(Math.Sqrt(-(2x - 3)*(2x - 1)) + 1) ; [0.5, 1]
    /// </summary>
    static public float CircularEaseInOut(float p)
    {
        if (p < 0.5f)
        {
            return 0.5f * (1 - Mathf.Sqrt(1 - 4 * (p * p)));
        }
        else
        {
            return 0.5f * (Mathf.Sqrt(-((2 * p) - 3) * ((2 * p) - 1)) + 1);
        }
    }
    /// <summary>
    /// </summary>
    static public float BounceEaseOut(float p)
    {
        if (p < 4 / 11.0f)
        {
            return (121 * p * p) / 16.0f;
        }
        else if (p < 8 / 11.0f)
        {
            return (363 / 40.0f * p * p) - (99 / 10.0f * p) + 17 / 5.0f;
        }
        else if (p < 9 / 10.0f)
        {
            return (4356 / 361.0f * p * p) - (35442 / 1805.0f * p) + 16061 / 1805.0f;
        }
        else
        {
            return (54 / 5.0f * p * p) - (513 / 25.0f * p) + 268 / 25.0f;
        }
    }
    /// <summary>
    /// </summary>
    static public float BounceEaseIn(float p)
    {
        return 1 - BounceEaseOut(1 - p);
    }
    /// <summary>
	/// </summary>
	static public float BounceEaseInOut(float p)
    {
        if (p < 0.5f)
        {
            return 0.5f * BounceEaseIn(p * 2);
        }
        else
        {
            return 0.5f * BounceEaseOut(p * 2 - 1) + 0.5f;
        }
    }
    /// <summary>
    /// Modeled after overshooting cubic y = 1-((1-x)^3-(1-x)*sin((1-x)*pi))
    /// </summary>	
    static public float BackEaseOut(float p)
    {
        float f = (1 - p);
        return 1 - (f * f * f - f * Mathf.Sin(f * PI));
    }
    /// <summary>
    /// Modeled after the damped sine wave y = sin(-13pi/2*(x + 1))*Math.Pow(2, -10x) + 1
    /// </summary>
    static public float ElasticEaseOut(float p)
    {
        return Mathf.Sin(-13 * HALFPI * (p + 1)) * Mathf.Pow(2, -10 * p) + 1;
    }
    #endregion

    #region PROJECTILE - OBJECT POOLING
    void ProjectileObjectPool()
    {
        // Object pooling process of the projectiles
        projectilePool = new Queue<GameObject>();
        for (int i = 0; i < projectilePoolSize; i++)
        {
            lastProjectile = Instantiate<GameObject>(projectile, poolHolderTrans);
            lastProjectileScript = lastProjectile.GetComponent<StandardizedProjectileForHand>();
            lastProjectileScript.bowScript = this;
            lastProjectileScript.quiver = poolHolderTrans;
            lastProjectileScript.PoolTheParticles();
            lastProjectileScript.rigid = lastProjectile.GetComponent<Rigidbody>();
            lastProjectile.SetActive(false);
            projectilePool.Enqueue(lastProjectile);
        }
    }
    // Add an extra projectile to the pool when needed.
    void AddExtraProjectileToPool()
    {
        lastProjectile = Instantiate<GameObject>(projectile, poolHolderTrans);
        lastProjectileScript = lastProjectile.GetComponent<StandardizedProjectileForHand>();
        lastProjectileScript.bowScript = this;
        lastProjectileScript.quiver = poolHolderTrans;
        lastProjectileScript.PoolTheParticles();
        lastProjectileScript.rigid = lastProjectile.GetComponent<Rigidbody>();
        lastProjectileRigidbody = lastProjectileScript.rigid;
        lastProjectile.SetActive(false);
        projectilePool.Enqueue(lastProjectile);
        projectilePoolSize++;
    }
    #endregion

    #region EDITOR-CUSTOM INSPECTOR
    // Automation for the joint placement in the inspector
    public void FindBoneRigs()
    {        
        bowUpJoint1 = transform.GetChild(0).GetChild(0);
        bowUpJoint2 = transform.GetChild(0).GetChild(0).GetChild(0);
        bowUpJoint3 = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        bowDownJoint1 = transform.GetChild(0).GetChild(1);
        bowDownJoint2 = transform.GetChild(0).GetChild(1).GetChild(0);
        bowDownJoint3 = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0);
        bowStringPoint = transform.GetChild(0).GetChild(2);
    }
    #endregion
}
