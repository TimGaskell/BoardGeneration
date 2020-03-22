using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class TextureArrayWizard : ScriptableWizard
{

    public Texture2D[] textures;

    /// <summary>
    /// Creates a wizard application in the editor. Can be found under the asset/create directory.
    /// </summary>
    [MenuItem("Assets/Create/Texture Array")]
    static void CreateWizard() {
        ScriptableWizard.DisplayWizard<TextureArrayWizard>("Create Texture Array", "Create");
    }

    /// <summary>
    /// On the Create button press of the wizard. Is used to create a texture array of inputed textures in the wizard. Samples the first texture
    /// to have all following textures uniform in their format. Creates a game asset which stores all of the textures in an array. 
    /// </summary>
    private void OnWizardCreate() {
        if (textures.Length == 0) {
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject("Save Texture Array", "Texture Array", "asset", "Save Texture Array");
        if (path.Length == 0) {
            return;
        }

        Texture2D t = textures[0];
        Texture2DArray textureArray = new Texture2DArray(t.width, t.height, textures.Length, t.format, t.mipmapCount > 1);
        textureArray.anisoLevel = t.anisoLevel;
        textureArray.filterMode = t.filterMode;
        textureArray.wrapMode = t.wrapMode;

        for (int i = 0; i < textures.Length; i++) {
            for (int m = 0; m < t.mipmapCount; m++) {
                Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
            }
        }
        AssetDatabase.CreateAsset(textureArray, path);
    }
}
