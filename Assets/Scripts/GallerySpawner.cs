using System;
using System.Collections.Generic;
using UnityEngine;

public class GallerySpawner : MonoBehaviour
{
    private const float CHUNK_SIZE = 22;

    public GalleryChunk[] galleryChunkPrefabs;
    public GalleryChunk specialChunk;
    public Transform player;

    private static readonly HashSet<Vector2Int> OFF_LIMITS_INDICES = new HashSet<Vector2Int> {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(2, 0),
        new Vector2Int(1, 1),
        new Vector2Int(2, 1),
        new Vector2Int(3, 1),
        new Vector2Int(1, -1),
        new Vector2Int(2, -1),
        new Vector2Int(3, -1),
        new Vector2Int(3, 0),
    };

    private static readonly HashSet<Vector2Int> SPECIAL_INDICES = new HashSet<Vector2Int> {
        new Vector2Int(-10, 0)
    };
    private GalleryChunkArray galleryChunkArray = new GalleryChunkArray();


    private void Update() {
        Vector2Int currentIndex = PlayerPosIndex;
        SetOtherIndices(currentIndex);
        for (int i = 0; i < otherIndices.Length; i++) {
            EnsureChunk(otherIndices[i]);
        }
    }

    private int lastChunkInstantiateFrame = 0;
    private void EnsureChunk(Vector2Int chunkIndex) {
        if (!OFF_LIMITS_INDICES.Contains(chunkIndex)) {
            GalleryChunk existingChunk = galleryChunkArray.Get(chunkIndex);
            if (existingChunk == null && lastChunkInstantiateFrame != Time.frameCount) {
                lastChunkInstantiateFrame = Time.frameCount;
                GalleryChunk newChunk = Instantiate(GetChunkPrefab(chunkIndex));
                newChunk.t.localRotation = Quaternion.Euler(new Vector3(0, 90 * UnityEngine.Random.Range(0, 4), 0));
                newChunk.MoveToPosition(IndexToPosition(chunkIndex));
                galleryChunkArray.Set(chunkIndex, newChunk);
            }
        }
    }

    private GalleryChunk GetChunkPrefab(Vector2Int chunkIndex) {
        if (SPECIAL_INDICES.Contains(chunkIndex)) {
            return specialChunk;
        } else {
            return galleryChunkPrefabs[UnityEngine.Random.Range(0, galleryChunkPrefabs.Length)];
        }
    }

    private static readonly Vector2Int[] otherIndices = new Vector2Int[4];
    private void SetOtherIndices(Vector2Int centerIndex) {
        otherIndices[0] = new Vector2Int(centerIndex.x, centerIndex.y + 1);
        otherIndices[1] = new Vector2Int(centerIndex.x, centerIndex.y - 1);
        otherIndices[2] = new Vector2Int(centerIndex.x + 1, centerIndex.y);
        otherIndices[3] = new Vector2Int(centerIndex.x - 1, centerIndex.y);
    }


    private Vector2Int PlayerPosIndex {
        get {
            Vector3 pos = player.position;
            return new Vector2Int(Mathf.RoundToInt(pos.x / CHUNK_SIZE), Mathf.RoundToInt(pos.z / CHUNK_SIZE));
        }
    }

    private static Vector3 IndexToPosition(Vector2Int chunkIndex) {
        return new Vector3(chunkIndex.x * CHUNK_SIZE, 0, chunkIndex.y * CHUNK_SIZE);
    }
}
