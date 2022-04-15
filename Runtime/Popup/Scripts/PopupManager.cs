using AurecasLib.Blur;
using AurecasLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;

namespace AurecasLib.Popup {

    public class PopupManager : MonoBehaviour {
        public static PopupManager Instance;
        [HideInInspector]
        public PopupWindow LoadedPopup;
        public BlurRenderer.ShaderParams blurParams;

        List<PopupWindow> loadedPopups;
        Dictionary<int, GameObject> createdCanvases;
        GameObject canvasPreset;
        bool initialized = false;

        Dictionary<string, IResourceLocation> resourceLocationMap;

        private void Awake() {

            if (Instance == null) {
                Instance = this;
                loadedPopups = new List<PopupWindow>();
                createdCanvases = new Dictionary<int, GameObject>();
                canvasPreset = transform.GetChild(0).gameObject;

                StartCoroutine(Initialize());

                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this) {
                Destroy(gameObject);
            }
        }

        public GameObject GenerateBlackScreen(GameObject canvas) {
            GameObject preto = new GameObject("Preto");
            preto.transform.SetParent(canvas.transform, false);
            RectTransform pretoRect = preto.AddComponent<RectTransform>();
            pretoRect.anchorMin = Vector3.zero;
            pretoRect.anchorMax = Vector3.one;
            pretoRect.offsetMin = Vector3.zero;
            pretoRect.offsetMax = Vector3.zero;
            Image pretoImage = preto.AddComponent<Image>();
            pretoImage.color = new Color(1, 1, 1, 0f);
            return preto;
        }

        public IEnumerator OpenPopupRoutine(string assetId, int layerOrder) {

            while (!initialized) yield return new WaitForEndOfFrame();
            //Abre uma tela preta só pra nao ficar sem resposta
            BlurRenderer blurRenderer = BlurRenderer.Create();
            Material mat = blurRenderer.GetBlur(blurParams);

            GameObject canvas;
            if (createdCanvases.ContainsKey(layerOrder)) {
                canvas = createdCanvases[layerOrder];
            }
            else {
                canvas = Instantiate(canvasPreset, transform);
                canvas.SetActive(true);
                Canvas cv = canvas.GetComponent<Canvas>();
                cv.sortingOrder = layerOrder;
                cv.renderMode = RenderMode.ScreenSpaceCamera;
                cv.worldCamera = Camera.main;
                createdCanvases.Add(layerOrder, canvas);
            }

            GameObject preto = GenerateBlackScreen(canvas);

            Task<GameObject> t = Addressables.InstantiateAsync(resourceLocationMap[assetId]).Task;

            while (!t.IsCompleted) {
                yield return null;
            }

            t.Result.transform.SetParent(canvas.transform, false);
            PopupWindow popup = t.Result.GetComponent<PopupWindow>();
            popup.SetBlurRenderer(blurRenderer, mat);
            popup.OpenPopup();
            LoadedPopup = popup;
            loadedPopups.Add(popup);

            Destroy(preto);

        }

        public IEnumerator OpenPopupRoutine(AssetReference assetReference, int layerOrder) {
            while (!initialized) yield return new WaitForEndOfFrame();
            //Abre uma tela preta só pra nao ficar sem resposta
            BlurRenderer blurRenderer = BlurRenderer.Create();
            Material mat = blurRenderer.GetBlur(blurParams);

            GameObject canvas;
            if (createdCanvases.ContainsKey(layerOrder)) {
                canvas = createdCanvases[layerOrder];
            }
            else {
                canvas = Instantiate(canvasPreset, transform);
                canvas.SetActive(true);
                Canvas cv = canvas.GetComponent<Canvas>();
                cv.sortingOrder = layerOrder;
                cv.renderMode = RenderMode.ScreenSpaceCamera;
                cv.worldCamera = Camera.main;
                createdCanvases.Add(layerOrder, canvas);
            }

            GameObject preto = GenerateBlackScreen(canvas);

            Task<GameObject> t = Addressables.InstantiateAsync(assetReference).Task;

            while (!t.IsCompleted) {
                yield return null;
            }

            t.Result.transform.SetParent(canvas.transform, false);
            PopupWindow popup = t.Result.GetComponent<PopupWindow>();
            popup.SetBlurRenderer(blurRenderer, mat);
            popup.OpenPopup();
            LoadedPopup = popup;
            loadedPopups.Add(popup);

            Destroy(preto);
        }

        private IEnumerator Initialize() {
            YieldableTask<IList<IResourceLocation>> task = new YieldableTask<IList<IResourceLocation>>(Addressables.LoadResourceLocationsAsync("popups").Task);
            yield return task;
            resourceLocationMap = new Dictionary<string, IResourceLocation>();
            foreach (IResourceLocation r in task.GetResult()) {
                if (resourceLocationMap.ContainsKey(r.PrimaryKey))
                    resourceLocationMap[r.PrimaryKey] = r;
                else resourceLocationMap.Add(r.PrimaryKey, r);

                if (resourceLocationMap.ContainsKey(r.ToString()))
                    resourceLocationMap[r.ToString()] = r;
                else resourceLocationMap.Add(r.ToString(), r);
            }
            initialized = true;
        }

        private void Update() {
            for (int i = loadedPopups.Count - 1; i >= 0; i--) {
                PopupWindow window = loadedPopups[i];
                if (!window.InScene()) {
                    loadedPopups.RemoveAt(i);
                    Addressables.Release(window.gameObject);
                }
            }

            List<int> canvasesToRemove = new List<int>();
            foreach (int layerOrder in createdCanvases.Keys) {
                if (createdCanvases[layerOrder].transform.childCount == 0) {
                    canvasesToRemove.Add(layerOrder);
                }
            }
            foreach (int c in canvasesToRemove) {
                Destroy(createdCanvases[c]);
                createdCanvases.Remove(c);
            }
        }

        public void OpenPopup(AssetReference assetReference, int layerOrder, Action<PopupWindow> callback) {
            StartCoroutine(_OpenPopup(assetReference, layerOrder, callback));
        }
        public void OpenPopup(string assetKey, int layerOrder, Action<PopupWindow> callback) {
            StartCoroutine(_OpenPopup(assetKey, layerOrder, callback));
        }
        private IEnumerator _OpenPopup(AssetReference assetReference, int layerOrder, Action<PopupWindow> callback) {
            yield return OpenPopupRoutine(assetReference, layerOrder);
            callback?.Invoke(LoadedPopup);
        }

        private IEnumerator _OpenPopup(string assetKey, int layerOrder, Action<PopupWindow> callback) {
            yield return OpenPopupRoutine(assetKey, layerOrder);
            callback?.Invoke(LoadedPopup);
        }
    }
}