using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MiniJasmine.Editor
{
    public enum SpriteSheetToFBFAnimationExportMode
    {
        SpriteRenderer,
        Image
    }

    public class SpriteSheetToAnimationEditor : EditorWindow
    {
        [MenuItem("MiniJasmine/Sprite Sheet To FBF Animation")]
        private static void OpenSpriteSheetToAnimationWindow()
        {
            var window = GetWindow<SpriteSheetToAnimationEditor>("Sprite Sheet To FBF Animation");
            window.minSize = new Vector2(300, 350);
        }

        private Texture2D selectedTexture = null;
        private int sampleRate = 60;
        private bool useSaveInsideSameFolder = true;
        private bool isReplacingName = false;
        private bool isLooping = false;
        private string customSavePath = "";
        private string updatedName = "";
        private SpriteSheetToFBFAnimationExportMode mode = SpriteSheetToFBFAnimationExportMode.SpriteRenderer;


        private void OnGUI()
        {
            selectedTexture = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", selectedTexture, typeof(Texture2D), false);

            string selectedTexturePath = AssetDatabase.GetAssetPath(selectedTexture);
            List<Sprite> selectedSprites = AssetDatabase.LoadAllAssetsAtPath(selectedTexturePath).OfType<Sprite>().ToList();
            if (selectedTexture == null || selectedSprites.Count == 0)
            {
                EditorGUILayout.HelpBox($"No Texture is selected!", MessageType.Error);
            }
            else if (selectedSprites.Count == 1)
            {
                EditorGUILayout.HelpBox($"Current Selected Texture has {selectedSprites.Count} sprites, is it intended?", MessageType.Warning);
            }
            else if (selectedSprites.Count > 1)
            {
                EditorGUILayout.HelpBox($"Selected Texture has {selectedSprites.Count} sprites", MessageType.Info);
            }


            mode = (SpriteSheetToFBFAnimationExportMode)EditorGUILayout.EnumPopup("Export Mode? ", mode);
            sampleRate = EditorGUILayout.IntField("Sample Rates", sampleRate);
            isLooping = EditorGUILayout.Toggle("Loops? ", isLooping);
            useSaveInsideSameFolder = EditorGUILayout.Toggle("Save inside texture folder? ", useSaveInsideSameFolder);

            if (!useSaveInsideSameFolder)
            {
                customSavePath = EditorGUILayout.TextField("Save Path", customSavePath);
            }

            isReplacingName = EditorGUILayout.Toggle("Rename? ", isReplacingName);

            if (isReplacingName)
            {
                updatedName = EditorGUILayout.TextField("Replaced Name", updatedName);
            }

            (string savePath, string clipName) = GetNameInfo();


            if (selectedTexture == null || string.IsNullOrEmpty(savePath))
            {
                GUI.enabled = false;
                GUILayout.Button("Generate");
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button("Generate"))
                {
                    Generate(savePath, clipName);
                }
                if (selectedTexture != null)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField($"Save path: {savePath}/{clipName}.anim");
                }
            }

        }

        private (string savePath, string clipName) GetNameInfo()
        {
            string savePath = "";
            string clipName = "";

            if (isReplacingName)
                clipName = updatedName;
            else if (selectedTexture != null)
                clipName = selectedTexture.name;

            if (!useSaveInsideSameFolder)
            {
                savePath = $"Assets/{customSavePath}";
            }
            else if (selectedTexture != null)
            {
                savePath = AssetDatabase.GetAssetPath(selectedTexture);
                savePath = savePath.Remove(savePath.LastIndexOf('/'), savePath.Length - savePath.LastIndexOf('/'));
            }

            return (savePath, clipName);
        }

        private void Generate(string savePath, string clipName)
        {
            string selectedTexturePath = AssetDatabase.GetAssetPath(selectedTexture);


            List<Sprite> selectedSprites = AssetDatabase.LoadAllAssetsAtPath(selectedTexturePath).OfType<Sprite>().ToList();

            AnimationClip clip = new AnimationClip();
            clip.name = clipName;

            AnimationClipSettings clipSettings = new AnimationClipSettings();
            clipSettings.loopTime = isLooping;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

            // Initialize the curve property for the animation clip
            EditorCurveBinding curveBinding = new EditorCurveBinding();
            curveBinding.propertyName = "m_Sprite";
            // Assumes user wants to apply the sprite property to the root element
            curveBinding.path = "";

            if (mode == SpriteSheetToFBFAnimationExportMode.SpriteRenderer)
                curveBinding.type = typeof(SpriteRenderer);
            else
                curveBinding.type = typeof(Image);


            ObjectReferenceKeyframe[] keys = CreateKeysForSprites(selectedSprites);

            // Build the clip if valid
            if (keys.Length > 0)
            {
                // Set the keyframes to the animation
                AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keys);
            }

            string assetSavePath = $"{savePath}/{clipName}.anim";

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(clip, assetSavePath);
            AssetDatabase.SaveAssets();
        }

        private ObjectReferenceKeyframe[] CreateKeysForSprites(List<Sprite> sprites)
        {
            List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();
            float timePerFrame = 1.0f / sampleRate;
            float currentTime = 0.0f;
            foreach (Sprite sprite in sprites)
            {
                ObjectReferenceKeyframe keyframe = new ObjectReferenceKeyframe();
                keyframe.time = currentTime;
                keyframe.value = sprite;
                keys.Add(keyframe);

                currentTime += timePerFrame;
            }

            return keys.ToArray();
        }
    }

}