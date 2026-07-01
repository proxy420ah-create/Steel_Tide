// Steel Tide: First Device — Voxels / .stasset Skeleton (v2)
// StAssetSkeleton.cs
//
// Typed model for the optional skeleton metadata block appended to .stasset v2
// files by the Voxel Asset Studio (see VoxelAssetStudio/stasset_io.py).
//
// Block layout (after the voxel payload):
//   "SKEL" (4 bytes) + uint32 LE json_length + UTF-8 JSON
//
// JSON payload:
//   {
//     "version": 2,
//     "root_joint": <int>,
//     "bones":  [ { id, name, start[3], end[3], length, parent_joint, child_joint }, ... ],
//     "joints": [ { id, name, type, position[3], (axis|min_angle|max_angle|max_angle_x/y/z) }, ... ],
//     "influence_map": { },
//     "attachments": [ ]
//   }
//
// Hierarchy convention (single tree rooted at root_joint, the pelvis):
//   parent_joint = joint nearer the root (the bone's pivot)
//   child_joint  = joint farther from the root (-1 here == JSON null == tip bone)
//   A bone's PARENT BONE is the bone whose child_joint == this bone's parent_joint.
//
// Unity ships no Newtonsoft by default and JsonUtility cannot represent the
// nullable ids / dictionary, so we use the small dependency-free MiniJson parser.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace SteelTide.Voxels
{
    public enum JointType { Root, Ball, Hinge }

    public class SkeletonJoint
    {
        public int id;
        public string name;
        public JointType type;
        public Vector3Int position;   // voxel-grid coordinates

        // Hinge limits (valid when type == Hinge).
        public string axis;           // "X" / "Y" / "Z"
        public float minAngle;
        public float maxAngle;

        // Ball limits (valid when type == Ball).
        public float maxAngleX;
        public float maxAngleY;
        public float maxAngleZ;
    }

    public class SkeletonBone
    {
        public const int NoJoint = -1;

        public int id;
        public string name;
        public string role;          // e.g. "pelvis", "spine", "thigh", "shin", ...
        public string side;          // "" / "L" / "R"
        public Vector3Int start;      // voxel-grid coordinates
        public Vector3Int end;        // voxel-grid coordinates
        public float length;
        public float mass;            // physics mass derived from material densities
        public int parentJoint = NoJoint;  // pivot joint nearer the root
        public int childJoint = NoJoint;   // joint farther from the root
    }

    /// <summary>Parsed skeleton metadata from a .stasset v2 file.</summary>
    public class VoxelSkeleton
    {
        public int rootJoint = SkeletonBone.NoJoint;
        public readonly List<SkeletonBone> bones = new List<SkeletonBone>();
        public readonly List<SkeletonJoint> joints = new List<SkeletonJoint>();

        private readonly Dictionary<int, SkeletonJoint> _jointById = new Dictionary<int, SkeletonJoint>();
        // child_joint id -> the bone that ends at it (used to find a bone's parent bone).
        private readonly Dictionary<int, SkeletonBone> _boneByChildJoint = new Dictionary<int, SkeletonBone>();

        public SkeletonJoint GetJoint(int id)
        {
            return _jointById.TryGetValue(id, out var j) ? j : null;
        }

        /// <summary>The parent bone of <paramref name="bone"/>, or null if it attaches to the root.</summary>
        public SkeletonBone GetParentBone(SkeletonBone bone)
        {
            if (bone == null || bone.parentJoint == SkeletonBone.NoJoint || bone.parentJoint == rootJoint)
                return null;
            return _boneByChildJoint.TryGetValue(bone.parentJoint, out var parent) ? parent : null;
        }

        private void BuildLookups()
        {
            _jointById.Clear();
            foreach (var j in joints)
                _jointById[j.id] = j;

            _boneByChildJoint.Clear();
            foreach (var b in bones)
                if (b.childJoint != SkeletonBone.NoJoint)
                    _boneByChildJoint[b.childJoint] = b;
        }

        public static VoxelSkeleton FromJson(string json)
        {
            object root;
            try
            {
                root = MiniJson.Parse(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoxelSkeleton] Failed to parse skeleton JSON: {e.Message}");
                return null;
            }

            if (!(root is Dictionary<string, object> obj))
                return null;

            var skel = new VoxelSkeleton
            {
                rootJoint = GetInt(obj, "root_joint", SkeletonBone.NoJoint)
            };

            if (obj.TryGetValue("joints", out var jointsObj) && jointsObj is List<object> jointList)
            {
                foreach (var item in jointList)
                {
                    if (item is Dictionary<string, object> jd)
                        skel.joints.Add(ParseJoint(jd));
                }
            }

            if (obj.TryGetValue("bones", out var bonesObj) && bonesObj is List<object> boneList)
            {
                foreach (var item in boneList)
                {
                    if (item is Dictionary<string, object> bd)
                        skel.bones.Add(ParseBone(bd));
                }
            }

            skel.BuildLookups();
            return skel;
        }

        private static SkeletonJoint ParseJoint(Dictionary<string, object> d)
        {
            var j = new SkeletonJoint
            {
                id = GetInt(d, "id", 0),
                name = GetString(d, "name", ""),
                type = ParseJointType(GetString(d, "type", "BALL")),
                position = GetVec3Int(d, "position"),
                axis = GetString(d, "axis", null),
                minAngle = GetFloat(d, "min_angle", 0f),
                maxAngle = GetFloat(d, "max_angle", 0f),
                maxAngleX = GetFloat(d, "max_angle_x", 0f),
                maxAngleY = GetFloat(d, "max_angle_y", 0f),
                maxAngleZ = GetFloat(d, "max_angle_z", 0f),
            };
            return j;
        }

        private static SkeletonBone ParseBone(Dictionary<string, object> d)
        {
            return new SkeletonBone
            {
                id = GetInt(d, "id", 0),
                name = GetString(d, "name", ""),
                role = GetString(d, "role", ""),
                side = GetString(d, "side", ""),
                start = GetVec3Int(d, "start"),
                end = GetVec3Int(d, "end"),
                length = GetFloat(d, "length", 0f),
                mass = GetFloat(d, "mass", 0f),
                parentJoint = GetInt(d, "parent_joint", SkeletonBone.NoJoint),
                childJoint = GetInt(d, "child_joint", SkeletonBone.NoJoint),
            };
        }

        private static JointType ParseJointType(string s)
        {
            if (string.IsNullOrEmpty(s)) return JointType.Ball;
            switch (s.ToUpperInvariant())
            {
                case "ROOT": return JointType.Root;
                case "HINGE": return JointType.Hinge;
                default: return JointType.Ball;
            }
        }

        // ---- typed accessors over the generic object graph ----

        private static int GetInt(Dictionary<string, object> d, string key, int fallback)
        {
            if (d.TryGetValue(key, out var v) && v != null)
                return Convert.ToInt32(Convert.ToDouble(v, CultureInfo.InvariantCulture));
            return fallback;
        }

        private static float GetFloat(Dictionary<string, object> d, string key, float fallback)
        {
            if (d.TryGetValue(key, out var v) && v != null)
                return (float)Convert.ToDouble(v, CultureInfo.InvariantCulture);
            return fallback;
        }

        private static string GetString(Dictionary<string, object> d, string key, string fallback)
        {
            if (d.TryGetValue(key, out var v) && v is string s)
                return s;
            return fallback;
        }

        private static Vector3Int GetVec3Int(Dictionary<string, object> d, string key)
        {
            if (d.TryGetValue(key, out var v) && v is List<object> list && list.Count >= 3)
            {
                return new Vector3Int(
                    (int)Math.Round(Convert.ToDouble(list[0], CultureInfo.InvariantCulture)),
                    (int)Math.Round(Convert.ToDouble(list[1], CultureInfo.InvariantCulture)),
                    (int)Math.Round(Convert.ToDouble(list[2], CultureInfo.InvariantCulture)));
            }
            return Vector3Int.zero;
        }
    }

    /// <summary>
    /// Minimal, dependency-free JSON parser. Returns a graph of
    /// Dictionary&lt;string,object&gt; / List&lt;object&gt; / string / double / bool / null.
    /// Sufficient for the .stasset skeleton block; not a general-purpose validator.
    /// </summary>
    internal static class MiniJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            int i = 0;
            object value = ParseValue(json, ref i);
            return value;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static object ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            char c = s[i];
            switch (c)
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return ParseString(s, ref i);
                case 't': i += 4; return true;
                case 'f': i += 5; return false;
                case 'n': i += 4; return null;
                default: return ParseNumber(s, ref i);
            }
        }

        private static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var obj = new Dictionary<string, object>();
            i++; // '{'
            SkipWhitespace(s, ref i);
            if (s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipWhitespace(s, ref i);
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                i++; // ':'
                obj[key] = ParseValue(s, ref i);
                SkipWhitespace(s, ref i);
                char c = s[i++];
                if (c == '}') break; // else ',' -> continue
            }
            return obj;
        }

        private static List<object> ParseArray(string s, ref int i)
        {
            var arr = new List<object>();
            i++; // '['
            SkipWhitespace(s, ref i);
            if (s[i] == ']') { i++; return arr; }
            while (true)
            {
                arr.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                char c = s[i++];
                if (c == ']') break; // else ',' -> continue
            }
            return arr;
        }

        private static string ParseString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // opening quote
            while (true)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c == '\\')
                {
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            string hex = s.Substring(i, 4); i += 4;
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            break;
                        default: sb.Append(e); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static object ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            string num = s.Substring(start, i - start);
            return double.Parse(num, CultureInfo.InvariantCulture);
        }
    }
}
