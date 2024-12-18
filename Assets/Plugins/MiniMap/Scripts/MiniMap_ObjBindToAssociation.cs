using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmagiSakuya.MiniMap
{
    public class MiniMap_ObjBindToAssociation : MonoBehaviour
    {
        [SerializeField] MiniMap map;
        [SerializeField] GameObject target;
        [SerializeField] string itemName;
        [SerializeField] float scaleInMinMap = 1f;
        [SerializeField] float scaleInWorldMap = 1.5f;
        public bool miniMapStrongPrompt;

        void OnEnable()
        {
            if (map == null)
            {
                map = FindObjectOfType<MiniMap>();
            }
            if (map == null) return;
            map.AddToAssociations(target == null ? gameObject : target, itemName, scaleInMinMap, scaleInWorldMap);
        }

        void OnDisable()
        {
            if (map == null) return;
            map.RemoveFromAssociations(target == null ? gameObject : target);
        }

        void Update()
        {
            if (map == null) return;
            map.FindAssociationByGameObject(target == null ? gameObject : target).miniMapStrongPrompt = miniMapStrongPrompt;
        }
    }
}