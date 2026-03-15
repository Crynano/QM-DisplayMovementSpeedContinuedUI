using MGSC;
using System.Collections.Generic;
using UnityEngine;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public class Pooler : MonoBehaviour
    {
        private Queue<DisplayMovementController> _availablePool = new Queue<DisplayMovementController>();
        private Dictionary<Creature, DisplayMovementController> _activeControllers = new Dictionary<Creature, DisplayMovementController>();
        private GameObject _prefab;
        private Transform _parent;

        private readonly int _initialPoolSize = 1;
        private readonly int _maxPoolSize = 50;

        private DisplayMovementBlackboard _blackboard;
 
        public void Initialize(GameObject prefab, Transform parent)
        {
            _blackboard = new DisplayMovementBlackboard();
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewController();
            }
        }

        private DisplayMovementController CreateNewController()
        {
            var instance = Instantiate(_prefab, _parent);
            var controller = instance.GetComponent<DisplayMovementController>();
            controller.LoadUI();
            controller.SetSprites(_blackboard.MeleeSprite, _blackboard.RangedSprite, _blackboard.DamageSprites);
            controller.DisableUI();

            _availablePool.Enqueue(controller);
            return controller;
        }

        public DisplayMovementController GetController(Monster monster)
        {
            if (_activeControllers.TryGetValue(monster, out DisplayMovementController existingController))
            {
                return existingController;
            }

            DisplayMovementController controller;
            
            if (_availablePool.Count > 0)
            {
                controller = _availablePool.Dequeue();
            }
            else
            {
                int totalCount = _availablePool.Count + _activeControllers.Count;
                if (totalCount < _maxPoolSize)
                {
                    controller = CreateNewController();
                    _availablePool.Dequeue();
                }
                else
                {
                    Logger.LogWarning($"DisplayMovementUIPool: Max pool size reached ({_maxPoolSize}). Reusing oldest controller.");
                    controller = GetOldestActiveController();
                    if (controller != null)
                    {
                        ReturnController(controller);
                    }
                    else
                    {
                        controller = CreateNewController();
                        _availablePool.Dequeue();
                    }
                }
            }

            _activeControllers[monster] = controller;
            return controller;
        }

        public void ReturnController(Creature monster)
        {
            if (_activeControllers.TryGetValue(monster, out DisplayMovementController controller))
            {
                controller.DisableUI();
                _activeControllers.Remove(monster);
                _availablePool.Enqueue(controller);
            }
        }

        private void ReturnController(DisplayMovementController controller)
        {
            Creature monsterToRemove = null;
            foreach (var kvp in _activeControllers)
            {
                if (kvp.Value == controller)
                {
                    monsterToRemove = kvp.Key;
                    break;
                }
            }

            if (monsterToRemove != null)
            {
                controller.DisableUI();
                _activeControllers.Remove(monsterToRemove);
                _availablePool.Enqueue(controller);
            }
        }

        public void ReturnAllControllers()
        {
            var monstersToRemove = new List<Creature>(_activeControllers.Keys);
            foreach (var monster in monstersToRemove)
            {
                ReturnController(monster);
            }
        }

        public bool IsMonsterActive(Monster monster)
        {
            return _activeControllers.ContainsKey(monster);
        }

        public DisplayMovementController GetActiveController(Monster monster)
        {
            _activeControllers.TryGetValue(monster, out DisplayMovementController controller);
            return controller;
        }

        private DisplayMovementController GetOldestActiveController()
        {
            foreach (var kvp in _activeControllers)
            {
                return kvp.Value;
            }
            return null;
        }

        public int ActiveCount => _activeControllers.Count;
        public int AvailableCount => _availablePool.Count;

        private void OnDestroy()
        {
            ReturnAllControllers();
        }
    }
}
