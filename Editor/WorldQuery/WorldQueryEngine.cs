using System;
using System.Collections.Generic;
using SkyWalker.Craft.Editor.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyWalker.Craft.Editor.WorldQuery
{
    /// <summary>
    /// Queries the scene hierarchy by name, component, tag, and parent path.
    /// MCP-agnostic — pure Unity API.
    /// </summary>
    public class WorldQueryEngine
    {
        public WorldQueryResult Query(WorldQueryRequest request)
        {
            var result = new WorldQueryResult();
            var candidates = new List<GameObject>();

            // Collect all root GameObjects from all loaded scenes
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    CollectAll(root, candidates);
                }
            }

            // Filter and score
            foreach (var go in candidates)
            {
                if (MatchesFilters(go, request))
                {
                    result.results.Add(ToQueryHit(go));
                    if (result.results.Count >= request.maxResults)
                        break;
                }
            }

            result.totalFound = result.results.Count;
            return result;
        }

        void CollectAll(GameObject go, List<GameObject> list)
        {
            list.Add(go);
            foreach (Transform child in go.transform)
            {
                CollectAll(child.gameObject, list);
            }
        }

        bool MatchesFilters(GameObject go, WorldQueryRequest request)
        {
            var filters = request.filters;
            if (filters == null) return MatchesQuery(go, request.query);

            // Name filter
            if (!string.IsNullOrEmpty(filters.name))
            {
                if (!go.name.Contains(filters.name, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Tag filter
            if (filters.tags != null && filters.tags.Count > 0)
            {
                bool tagMatch = false;
                foreach (var tag in filters.tags)
                {
                    if (go.CompareTag(tag)) { tagMatch = true; break; }
                }
                if (!tagMatch) return false;
            }

            // Component filter
            if (filters.components != null && filters.components.Count > 0)
            {
                foreach (var compName in filters.components)
                {
                    if (!HasComponent(go, compName))
                        return false;
                }
            }

            // Parent filter
            if (!string.IsNullOrEmpty(filters.parent))
            {
                var path = GetGameObjectPath(go);
                if (!path.StartsWith(filters.parent, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // General query (if filters also have a query string)
            if (!string.IsNullOrEmpty(request.query))
            {
                return MatchesQuery(go, request.query);
            }

            return true;
        }

        bool MatchesQuery(GameObject go, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            return go.name.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        bool HasComponent(GameObject go, string componentName)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var t = comp.GetType();
                if (t.Name == componentName || t.FullName == componentName)
                    return true;
            }
            return false;
        }

        QueryHit ToQueryHit(GameObject go)
        {
            var components = new List<string>();
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp != null)
                    components.Add(comp.GetType().Name);
            }

            var t = go.transform;
            return new QueryHit
            {
                path = GetGameObjectPath(go),
                name = go.name,
                instanceId = go.GetInstanceID(),
                components = components,
                tag = go.tag,
                layer = go.layer,
                activeInHierarchy = go.activeInHierarchy,
                transform = new TransformData
                {
                    position = new[] { t.position.x, t.position.y, t.position.z },
                    rotation = new[] { t.eulerAngles.x, t.eulerAngles.y, t.eulerAngles.z },
                    scale = new[] { t.localScale.x, t.localScale.y, t.localScale.z }
                }
            };
        }

        static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }
}
