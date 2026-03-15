using System.IO;
using UnityEditor;
using UnityEngine;

namespace MGSC
{
    public class ActorAutoPacker
    {
        private const string ACTORS_ROOT_PATH = "Assets/Content/Actors";
        private const string PACKED_ACTORS_PATH = "Assets/Resources/PackedActors";

        [MenuItem("MGSC/Tools/Repack All Actor Meshes")]
        public static void AutoPackAllMeshes()
        {
            ClearPackedActorFiles();

            var actorFolders = Directory.GetDirectories(ACTORS_ROOT_PATH, "*", SearchOption.AllDirectories);

            foreach (var folder in actorFolders)
            {
                if (Path.GetFileName(folder).Equals("template", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var descriptorPath = Path.Combine(folder, "view.asset");
                
                if (!File.Exists(descriptorPath)) 
                {
                    Debug.LogWarning($"No 'view.asset' found in: {folder}");
                    continue;
                }

                var actorDescriptor = AssetDatabase.LoadAssetAtPath<ActorDescriptor>(descriptorPath);

                if (actorDescriptor == null)
                {
                    Debug.LogError($"Failed to load ActorDescriptor at: {descriptorPath}");
                    continue;
                }

                ActorEditor.AssignBonesIds(actorDescriptor.Creature3DModel);
                ActorEditor.AssignAnimationCollections(actorDescriptor);
                ActorEditor.PackMesh(actorDescriptor);
            }

            AssetDatabase.Refresh();
            Debug.Log("All Actor Meshes have been packed successfully!");
        }
        
        private static void ClearPackedActorFiles()
        {
            if (Directory.Exists(PACKED_ACTORS_PATH))
            {
                var files = Directory.GetFiles(PACKED_ACTORS_PATH, "*.bytes", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    File.Delete(file);
                }

                Debug.Log($"Cleared {files.Length} packed actor files from: {PACKED_ACTORS_PATH}");
            }
            else
            {
                Directory.CreateDirectory(PACKED_ACTORS_PATH);
                Debug.Log($"Created directory: {PACKED_ACTORS_PATH}");
            }
        }
    }
}