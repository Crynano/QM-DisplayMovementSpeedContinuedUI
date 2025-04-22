using MGSC;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUIPermanent
{
    public class DisplayMovementUIPooler : MonoBehaviour
    {
        Transform canvasRoot;
        GameObject uiPrefab;

        private Dictionary<Monster, DisplayMovementController> register;
        private List<DisplayMovementController> pooledObjects => register.Values.ToList();

        public void LoadDungeon()
        {
            register = new Dictionary<Monster, DisplayMovementController>();
            canvasRoot = this.gameObject.transform;
            uiPrefab = DataLoader.LoadFileFromBundle<GameObject>("apcontrollerbundle", "ControllerPrefab");

            CreateInstancesForAllVisibleEnemies();
        }

        public void UnloadDungeon()
        {
            foreach (var item in pooledObjects)
            {
                Destroy(item.gameObject);
            }
        }

        // On dungeon load, this must be executed?
        public void CreateInstancesForAllVisibleEnemies()
        {
            // Create instances for all the active mobs.
            var enemies = GameObject.FindObjectsOfType<Monster>().ToList();
            foreach (var monster in enemies)
            {
                GetInstance(monster);
            }
        }

        //public void ForceUpdateAll()
        //{
        //    foreach (var item in pooledObjects)
        //    {
        //        item.UpdateElement();
        //    }
        //}

        public DisplayMovementController GetInstance(Monster monster)
        {
            register.TryGetValue(monster, out var controller);
            if (controller != null && !monster.CreatureData.Health.Dead)
            {
                return controller;
            }
            else if (controller != null)
            {
                controller.SetEnemy(monster);
                return controller;
            }
            else
            {
#if DEBUG
                Debug.Log("Selected object is null, creating an empty one");
#endif
                var a = CreateInstance();
                pooledObjects.Add(a);
                a.SetEnemy(monster);
                register.Add(monster, a);
                return a;
            }
        }

        private DisplayMovementController CreateInstance()
        {
            if (uiPrefab == null)
            {
#if DEBUG
                Debug.LogError($"Could not spawn, UI PREFAB is null");
#endif
            }
            else if (canvasRoot != null)
            {
                DisplayMovementController uiController;
                uiController = GameObject.Instantiate(uiPrefab, canvasRoot).AddComponent<DisplayMovementController>();
                uiController.transform.SetAsFirstSibling();
                uiController.LoadComponents("apcontrollerbundle");
                uiController.name = $"[UI] DisplayMovement Controller";
                uiController.DisableUI();
#if DEBUG
                Debug.Log($"UI for DisplayMovement Controller has instantiated correctly");
#endif
                return uiController;
            }
            return null;
        }

        public void OnDestroy()
        {
            UnloadDungeon();
        }
    }
}
