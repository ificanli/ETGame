using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DamageNumbersPro;

namespace DamageNumbersPro.Internal
{
    [CreateAssetMenu(fileName = "Preset", menuName = "TextMeshPro/Preset for DNP", order = -1)]
    public class DNPPreset : ScriptableObject
    {
        //Font:
        public bool changeFontAsset;
        public TMP_FontAsset fontAsset;

        //Color:
        public bool changeColor;
        public Color color = Color.white;
        public bool enableGradient;
        public VertexGradient gradient = new VertexGradient(Color.white, Color.white, Color.white, Color.white);

        //Number:
        public bool changeNumber;
        public bool enableNumber = true;
        public TextSettings numberSettings = new TextSettings(0);
        public DigitSettings digitSettings = new DigitSettings(0);

        //Left Text:
        public bool changeLeftText;
        public bool enableLeftText = true;
        public string leftText;
        public TextSettings leftTextSettings = new TextSettings(0f);

        //Right Text:
        public bool changeRightText;
        public bool enableRightText = true;
        public string rightText;
        public TextSettings rightTextSettings = new TextSettings(0f);

        //Vertical Text:
        public bool hideVerticalTexts = false;

        //淡入:
        public bool changeFadeIn = false;
        public float durationFadeIn = 0.2f;
        public bool enableOffsetFadeIn = true;
        [Tooltip("TextA和TextB从这个偏移量一起移动。")]
        public Vector2 offsetFadeIn = new Vector2(0.5f, 0);
        public bool enableScaleFadeIn = true;
        [Tooltip("按这个比例缩放。")]
        public Vector2 scaleFadeIn = new Vector2(2, 2);
        public bool enableCrossScaleFadeIn = false;
        [Tooltip("从这个尺度缩放TextA，从这个尺度的倒数缩放TextB。")]
        public Vector2 crossScaleFadeIn = new Vector2(1, 1.5f);
        public bool enableShakeFadeIn = false;
        [Tooltip("震动偏移量")]
        public Vector2 shakeOffsetFadeIn = new Vector2(0, 1.5f);
        [Tooltip("震动频率")]
        public float shakeFrequencyFadeIn = 4f;

        //淡出:
        public bool changeFadeOut = false;
        public float durationFadeOut = 0.2f;
        public bool enableOffsetFadeOut = true;
        [Tooltip("TextA和TextB分开移动到这个偏移量。")]
        public Vector2 offsetFadeOut = new Vector2(0.5f, 0);
        public bool enableScaleFadeOut = false;
        [Tooltip("按这个比例放大。")]
        public Vector2 scaleFadeOut = new Vector2(2, 2);
        public bool enableCrossScaleFadeOut = false;
        [Tooltip("将TextA向外缩放到这个比例，并将TextB缩放到这个比例的倒数。")]
        public Vector2 crossScaleFadeOut = new Vector2(1, 1.5f);
        public bool enableShakeFadeOut = false;
        [Tooltip("震动偏移量")]
        public Vector2 shakeOffsetFadeOut = new Vector2(0, 1.5f);
        [Tooltip("震动频率")]
        public float shakeFrequencyFadeOut = 4f;

        //Movement:
        public bool changeMovement = false;
        public bool enableLerp = true;
        public LerpSettings lerpSettings = new LerpSettings(0);
        public bool enableVelocity = false;
        public VelocitySettings velocitySettings = new VelocitySettings(0);
        public bool enableShaking = false;
        [Tooltip("在空闲时摇动设置。")]
        public ShakeSettings shakeSettings = new ShakeSettings(new Vector2(0.005f, 0.005f));
        public bool enableFollowing = false;
        public FollowSettings followSettings = new FollowSettings(0);

        //Rotation:
        public bool changeRotation = false;
        public bool enableStartRotation = false;
        [Tooltip("随机刷出旋转的最小z角。")]
        public float minRotation = -4f;
        [Tooltip("随机刷出旋转的最大z角。")]
        public float maxRotation = 4f;
        public bool enableRotateOverTime = false;
        [Tooltip("z角的最小旋转速度。")]
        public float minRotationSpeed = -15f;
        [Tooltip("z角的最大旋转速度。")]
        public float maxRotationSpeed = 15;
        [Tooltip("定义生命周期内的旋转速度。")]
        public AnimationCurve rotateOverTime = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(0.4f, 1), new Keyframe(0.8f, 0), new Keyframe(1, 0) });

        //Scaling:
        public bool changeScaling = false;
        public bool enableScaleByNumber = false;
        public ScaleByNumberSettings scaleByNumberSettings = new ScaleByNumberSettings(0);
        public bool enableScaleOverTime = false;
        [Tooltip("将在它的生命周期中使用这条曲线进行缩放。")]
        public AnimationCurve scaleOverTime = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.7f));

        //Spam Control:
        public bool changeSpamControl = false;
        public string spamGroup = "";
        public bool enableCombination = false;
        public CombinationSettings combinationSettings = new CombinationSettings(0);
        public bool enableDestruction = false;
        public DestructionSettings destructionSettings = new DestructionSettings(0);
        public bool enableCollision = false;
        public CollisionSettings collisionSettings = new CollisionSettings(0);
        public bool enablePush = false;
        public PushSettings pushSettings = new PushSettings(0);

        public bool IsApplied(DamageNumber dn)
        {
            TMP_Text[] textMeshs = dn.GetTextMeshs();

            if (textMeshs[0] == null)
            {
                dn.GetReferencesIfNecessary();
                textMeshs = dn.GetTextMeshs();
            }

            bool isApplied = true;

            //Font:
            if (changeFontAsset)
            {
                foreach(TMP_Text tmp in textMeshs)
                {
                    if (fontAsset != tmp.font)
                    {
                        isApplied = false;
                    }
                }
            }

            //Color:
            if (changeColor)
            {
                foreach (TMP_Text tmp in textMeshs)
                {
                    if (color != tmp.color || enableGradient != tmp.enableVertexGradient || !gradient.Equals(tmp.colorGradient))
                    {
                        isApplied = false;
                    }
                }
            }

            //Number:
            if(changeNumber)
            {
                if(enableNumber != dn.enableNumber || !numberSettings.Equals(dn.numberSettings) || !digitSettings.Equals(dn.digitSettings))
                {
                    isApplied = false;
                }
            }

            //Left Text:
            if(changeLeftText)
            {
                if(enableLeftText != dn.enableLeftText || !leftTextSettings.Equals(dn.leftTextSettings) || leftText != dn.leftText)
                {
                    isApplied = false;
                }
            }

            //Right Text:
            if (changeRightText)
            {
                if (enableRightText != dn.enableRightText || !rightTextSettings.Equals(dn.rightTextSettings) || rightText != dn.rightText)
                {
                    isApplied = false;
                }
            }

            //Vertical Texts:
            if (hideVerticalTexts)
            {
                if(dn.enableTopText || dn.enableBottomText)
                {
                    isApplied = false;
                }
            }

            //淡入:
            if(changeFadeIn)
            {
                if(durationFadeIn != dn.durationFadeIn || enableOffsetFadeIn != dn.enableOffsetFadeIn || offsetFadeIn != dn.offsetFadeIn ||
                    enableScaleFadeIn != dn.enableScaleFadeIn || scaleFadeIn != dn.scaleFadeIn || enableCrossScaleFadeIn != dn.enableCrossScaleFadeIn ||
                    crossScaleFadeIn != dn.crossScaleFadeIn || enableShakeFadeIn != dn.enableShakeFadeIn || shakeOffsetFadeIn != dn.shakeOffsetFadeIn ||
                    shakeFrequencyFadeIn != dn.shakeFrequencyFadeIn)
                {
                    isApplied = false;
                }
            }

            //淡出:
            if (changeFadeOut)
            {
                if (durationFadeOut != dn.durationFadeOut || enableOffsetFadeOut != dn.enableOffsetFadeOut || offsetFadeOut != dn.offsetFadeOut ||
                    enableScaleFadeOut != dn.enableScaleFadeOut || scaleFadeOut != dn.scaleFadeOut || enableCrossScaleFadeOut != dn.enableCrossScaleFadeOut ||
                    crossScaleFadeOut != dn.crossScaleFadeOut || enableShakeFadeOut != dn.enableShakeFadeOut || shakeOffsetFadeOut != dn.shakeOffsetFadeOut ||
                    shakeFrequencyFadeOut != dn.shakeFrequencyFadeOut)
                {
                    isApplied = false;
                }
            }

            //Movement:
            if(changeMovement)
            {
                if(enableLerp != dn.enableLerp || !lerpSettings.Equals(dn.lerpSettings) ||
                    enableVelocity != dn.enableVelocity || !velocitySettings.Equals(dn.velocitySettings) ||
                    enableShaking != dn.enableShaking || !shakeSettings.Equals(dn.shakeSettings) ||
                    enableFollowing != dn.enableFollowing || !followSettings.Equals(dn.followSettings))
                {
                    isApplied = false;
                }
            }

            //Rotation:
            if(changeRotation)
            {
                if(enableStartRotation != dn.enableStartRotation || minRotation != dn.minRotation || maxRotation != dn.maxRotation ||
                    enableRotateOverTime != dn.enableRotateOverTime || minRotationSpeed != dn.minRotationSpeed || maxRotationSpeed != dn.maxRotationSpeed || !rotateOverTime.Equals(dn.rotateOverTime))
                {
                    isApplied = false;
                }
            }

            //Scale:
            if(changeScaling)
            {
                if(enableScaleByNumber != dn.enableScaleByNumber || !scaleByNumberSettings.Equals(dn.scaleByNumberSettings) ||
                    enableScaleOverTime != dn.enableScaleOverTime || !scaleOverTime.Equals(dn.scaleOverTime))
                {
                    isApplied = false;
                }
            }

            //Spam Group:
            if(changeSpamControl)
            {
                if(enableCombination != dn.enableCombination || !combinationSettings.Equals(dn.combinationSettings) ||
                    enableDestruction != dn.enableDestruction || !destructionSettings.Equals(dn.destructionSettings) ||
                    enableCollision != dn.enableCollision || !collisionSettings.Equals(dn.collisionSettings) ||
                    enablePush != dn.enablePush || !pushSettings.Equals(dn.pushSettings))
                {
                    isApplied = false;
                }
            }

            return isApplied;
        }

        public void Apply(DamageNumber dn)
        {
            TMP_Text[] textMeshs = dn.GetTextMeshs();

            //Font:
            if (changeFontAsset)
            {
                foreach(TMP_Text tmp in textMeshs)
                {
                    tmp.font = fontAsset;
                }
            }

            //Color:
            if (changeColor)
            {
                foreach (TMP_Text tmp in textMeshs)
                {
                    tmp.color = color;
                    tmp.enableVertexGradient = enableGradient;
                    tmp.colorGradient = gradient;
                }
            }

            //Number:
            if (changeNumber)
            {
                dn.enableNumber = enableNumber;
                dn.numberSettings = numberSettings;
                dn.digitSettings = digitSettings;
            }

            //Left Text:
            if (changeLeftText)
            {
                dn.enableLeftText = enableLeftText;
                dn.leftText = leftText;
                dn.leftTextSettings = leftTextSettings;
            }

            //Right Text:
            if (changeRightText)
            {
                dn.enableRightText = enableRightText;
                dn.rightText = rightText;
                dn.rightTextSettings = rightTextSettings;
            }

            //Hide Vertical Texts:
            if(hideVerticalTexts)
            {
                dn.enableTopText = dn.enableBottomText = false;
            }

            //淡入:
            if(changeFadeIn)
            {
                dn.durationFadeIn = durationFadeIn;
                dn.enableOffsetFadeIn = enableOffsetFadeIn;
                dn.offsetFadeIn = offsetFadeIn;
                dn.enableScaleFadeIn = enableScaleFadeIn;
                dn.scaleFadeIn = scaleFadeIn;
                dn.enableCrossScaleFadeIn = enableCrossScaleFadeIn;
                dn.crossScaleFadeIn = crossScaleFadeIn;
                dn.enableShakeFadeIn = enableShakeFadeIn;
                dn.shakeOffsetFadeIn = shakeOffsetFadeIn;
                dn.shakeFrequencyFadeIn = shakeFrequencyFadeIn;
            }

            //淡出:
            if (changeFadeOut)
            {
                dn.durationFadeOut = durationFadeOut;
                dn.enableOffsetFadeOut = enableOffsetFadeOut;
                dn.offsetFadeOut = offsetFadeOut;
                dn.enableScaleFadeOut = enableScaleFadeOut;
                dn.scaleFadeOut = scaleFadeOut;
                dn.enableCrossScaleFadeOut = enableCrossScaleFadeOut;
                dn.crossScaleFadeOut = crossScaleFadeOut;
                dn.enableShakeFadeOut = enableShakeFadeOut;
                dn.shakeOffsetFadeOut = shakeOffsetFadeOut;
                dn.shakeFrequencyFadeOut = shakeFrequencyFadeOut;
            }

            //Movement:
            if(changeMovement)
            {
                dn.enableLerp = enableLerp;
                dn.lerpSettings = lerpSettings;
                dn.enableVelocity = enableVelocity;
                dn.velocitySettings = velocitySettings;
                dn.enableShaking = enableShaking;
                dn.shakeSettings = shakeSettings;
                dn.enableFollowing = enableFollowing;
                dn.followSettings = followSettings;
            }

            //Rotation:
            if(changeRotation)
            {
                dn.enableStartRotation = enableStartRotation;
                dn.minRotation = minRotation;
                dn.maxRotation = maxRotation;
                dn.enableRotateOverTime = enableRotateOverTime;
                dn.minRotationSpeed = minRotationSpeed;
                dn.maxRotationSpeed = maxRotationSpeed;
                dn.rotateOverTime = rotateOverTime;
            }

            //Scale:
            if(changeScaling)
            {
                dn.enableScaleByNumber = enableScaleByNumber;
                dn.scaleByNumberSettings = scaleByNumberSettings;
                dn.enableScaleOverTime = enableScaleOverTime;
                dn.scaleOverTime = scaleOverTime;
            }

            //Spam Control:
            if(changeSpamControl)
            {
                if(dn.spamGroup == null || dn.spamGroup == "")
                {
                    dn.spamGroup = spamGroup;
                }

                dn.enableCombination = enableCombination;
                dn.combinationSettings = combinationSettings;
                dn.enableDestruction = enableDestruction;
                dn.destructionSettings = destructionSettings;
                dn.enableCollision = enableCollision;
                dn.collisionSettings = collisionSettings;
                dn.enablePush = enablePush;
                dn.pushSettings = pushSettings;
            }
        }

        public void Get(DamageNumber dn)
        {
            TMP_Text[] textMeshs = dn.GetTextMeshs();

            //Font:
            changeFontAsset = true;
            foreach (TMP_Text tmp in textMeshs)
            {
                if(tmp != null)
                {
                    fontAsset = tmp.font;
                }
            }

            //Color:
            changeColor = true;
            foreach (TMP_Text tmp in textMeshs)
            {
                if(tmp != null)
                {
                    color = tmp.color;
                    enableGradient = tmp.enableVertexGradient;
                    gradient = tmp.colorGradient;
                }
            }

            //淡入:
            changeFadeIn = true;
            durationFadeIn = dn.durationFadeIn;
            enableOffsetFadeIn = dn.enableOffsetFadeIn;
            offsetFadeIn = dn.offsetFadeIn;
            enableScaleFadeIn = dn.enableScaleFadeIn;
            scaleFadeIn = dn.scaleFadeIn;
            enableCrossScaleFadeIn = dn.enableCrossScaleFadeIn;
            crossScaleFadeIn = dn.crossScaleFadeIn;
            enableShakeFadeIn = dn.enableShakeFadeIn;
            shakeOffsetFadeIn = dn.shakeOffsetFadeIn;
            shakeFrequencyFadeIn = dn.shakeFrequencyFadeIn;

            //淡出:
            changeFadeOut = true;
            durationFadeOut = dn.durationFadeOut;
            enableOffsetFadeOut = dn.enableOffsetFadeOut;
            offsetFadeOut = dn.offsetFadeOut;
            enableScaleFadeOut = dn.enableScaleFadeOut;
            scaleFadeOut = dn.scaleFadeOut;
            enableCrossScaleFadeOut = dn.enableCrossScaleFadeOut;
            crossScaleFadeOut = dn.crossScaleFadeOut;
            enableShakeFadeOut = dn.enableShakeFadeOut;
            shakeOffsetFadeOut = dn.shakeOffsetFadeOut;
            shakeFrequencyFadeOut = dn.shakeFrequencyFadeOut;

            //Movement:
            changeMovement = true;
            enableLerp = dn.enableLerp;
            lerpSettings = dn.lerpSettings;
            enableVelocity = dn.enableVelocity;
            velocitySettings = dn.velocitySettings;
            enableShaking = dn.enableShaking;
            shakeSettings = dn.shakeSettings;
            enableFollowing = dn.enableFollowing;
            followSettings = dn.followSettings;

            //Rotation:
            changeRotation = true;
            enableStartRotation = dn.enableStartRotation;
            minRotation = dn.minRotation;
            maxRotation = dn.maxRotation;
            enableRotateOverTime = dn.enableRotateOverTime;
            minRotationSpeed = dn.minRotationSpeed;
            maxRotationSpeed = dn.maxRotationSpeed;
            rotateOverTime = dn.rotateOverTime;

            //Scale:
            changeScaling = true;
            enableScaleByNumber = dn.enableScaleByNumber;
            scaleByNumberSettings = dn.scaleByNumberSettings;
            enableScaleOverTime = dn.enableScaleOverTime;
            scaleOverTime = dn.scaleOverTime;

            //Spam Group:
            changeSpamControl = true;
            spamGroup = dn.spamGroup != "" ? "Default" : "";
            enableCombination = dn.enableCombination;
            combinationSettings = dn.combinationSettings;
            enableDestruction = dn.enableDestruction;
            destructionSettings = dn.destructionSettings;
            enableCollision = dn.enableCollision;
            collisionSettings = dn.collisionSettings;
            enablePush = dn.enablePush;
            pushSettings = dn.pushSettings;
        }
    }
}
