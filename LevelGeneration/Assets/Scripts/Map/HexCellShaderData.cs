using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCellShaderData : MonoBehaviour
{
	Texture2D cellTexture;
	Color32[] cellTextureData;

	List<HexCell> transitioningCells = new List<HexCell>();
	const float transitionSpeed = 255f;

	bool needsVisibilityReset;

	public HexGrid Grid { get; set; }

	public bool ImmediateMode { get; set; }

	/// <summary>
	/// Creates a global texture which is used to texture each cell on the map. Each cell has its individual color32 saved in an array  to apply the right color to each cell.
	/// </summary>
	/// <param name="x"> X size of map </param>
	/// <param name="z"> Z size of map </param>
	public void Initialize(int x, int z) {
		if (cellTexture) {
			cellTexture.Resize(x, z);
		}
		else {
			cellTexture = new Texture2D(
				x, z, TextureFormat.RGBA32, false, true
			);
			cellTexture.filterMode = FilterMode.Point;
			cellTexture.wrapMode = TextureWrapMode.Clamp;
			Shader.SetGlobalTexture("_HexCellData", cellTexture);
		}
		Shader.SetGlobalVector("_HexCellData_TexelSize",new Vector4(1f / x, 1f / z, x, z)
		);

		if (cellTextureData == null || cellTextureData.Length != x * z) {
			cellTextureData = new Color32[x * z];
		}
		else {
			for (int i = 0; i < cellTextureData.Length; i++) {
				cellTextureData[i] = new Color32(0, 0, 0, 0);
			}
		}

		transitioningCells.Clear();
		enabled = true;
	}

	/// <summary>
	/// Assigns the alpha of a hex cell location in the cellTextureData to the terrain type index to be used by the shader. This is what textures the cells to what terrain they are. 
	/// </summary>
	/// <param name="cell"> Cell that is being textured </param>
	public void RefreshTerrain(HexCell cell) {
		cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
		enabled = true;
	}

	void LateUpdate() {

		if (needsVisibilityReset) {
			needsVisibilityReset = false;
			Grid.ResetVisibility();
		}
		
		int delta = (int)(Time.deltaTime * transitionSpeed);
		if (delta == 0) {
			delta = 1;
		}
		for (int i = 0; i < transitioningCells.Count; i++) {
			if (!UpdateCellData(transitioningCells[i], delta)) {
				transitioningCells[i--] = transitioningCells[transitioningCells.Count - 1];
				transitioningCells.RemoveAt(transitioningCells.Count - 1);
			}
		}

		cellTexture.SetPixels32(cellTextureData);
		cellTexture.Apply();
		enabled = transitioningCells.Count > 0;
	}

	/// <summary>
	/// Assigns the Red color to equal the amount of visibility a cell has. If the Red channel is 0 then there is no visibility, else there is. 
	/// </summary>
	/// <param name="cell"> Cell that is being textured </param>
	public void RefreshVisibility(HexCell cell) {
		int index = cell.Index;
		if (ImmediateMode) {
			cellTextureData[index].r = cell.IsVisible ? (byte)255 : (byte)0;
			cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;
		}
		else if (cellTextureData[index].b != 255) {
			cellTextureData[index].b = 255;
			transitioningCells.Add(cell);
		}
		enabled = true;
	}

	/// <summary>
	/// Updates cells visibility in the shader. Slowly transitions lighting to full intensity over time. 
	/// </summary>
	/// <param name="cell"> Cell being updated </param>
	/// <param name="delta"> Time </param>
	/// <returns> Bool of whether the cell still needs to be updated again </returns>
	bool UpdateCellData(HexCell cell, int delta) {
		int index = cell.Index;
		Color32 data = cellTextureData[index];
		bool stillUpdating = false;

		if (cell.IsExplored && data.g < 255) {
			stillUpdating = true;
			int t = data.g + delta;
			data.g = t >= 255 ? (byte)255 : (byte)t;
		}

		if (cell.IsVisible) {
			if (data.r < 255) {
				stillUpdating = true;
				int t = data.r + delta;
				data.r = t >= 255 ? (byte)255 : (byte)t;
			}
		}
		else if (data.r > 0) {
			stillUpdating = true;
			int t = data.r - delta;
			data.r = t < 0 ? (byte)0 : (byte)t;
		}

		if (!stillUpdating) {
			data.b = 0;
		}
		cellTextureData[index] = data;
		return stillUpdating;
	}

	/// <summary>
	/// Resets visibility values for all cells
	/// </summary>
	public void ViewElevationChanged() {
		needsVisibilityReset = true;
		enabled = true;
	}
}