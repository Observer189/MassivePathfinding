using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtension 
{
   public static Vector3Int Round(this Vector3 vector)
   {
      return new Vector3Int(Mathf.RoundToInt(vector.x),Mathf.RoundToInt(vector.y),Mathf.RoundToInt(vector.z));
   }
   
   public static Vector2Int Round(this Vector2 vector)
   {
      return new Vector2Int(Mathf.RoundToInt(vector.x),Mathf.RoundToInt(vector.y));
   }
   
   public static Vector3Int Floor(this Vector3 vector)
   {
      return new Vector3Int(Mathf.FloorToInt(vector.x),Mathf.FloorToInt(vector.y),Mathf.FloorToInt(vector.z));
   }
   
   public static Vector2Int Floor(this Vector2 vector)
   {
      return new Vector2Int(Mathf.FloorToInt(vector.x),Mathf.FloorToInt(vector.y));
   }

   public static Vector3 ToVector3(this Vector3Int vector)
   {
      return new Vector3(vector.x,vector.y,vector.z);
   }
   
   public static Vector2 ToVector2(this Vector2Int vector)
   {
      return new Vector2(vector.x,vector.y);
   }
   
   public static Vector2 Rotate(this Vector2 vector, float angleInDegrees)
   {
      float sin = Mathf.Sin(angleInDegrees * Mathf.Deg2Rad);
      float cos = Mathf.Cos(angleInDegrees * Mathf.Deg2Rad);
      float tx = vector.x;
      float ty = vector.y;
      vector.x = (cos * tx) - (sin * ty);
      vector.y = (sin * tx) + (cos * ty);
      return vector;
   }
   
   public static int ManhattanMagnitude(this Vector2Int vector)
   {
      return Mathf.Abs(vector.x) + Mathf.Abs(vector.y);
   }
   
   public static Vector2Int Absolute(this Vector2Int vector)
   {
      return new Vector2Int(Mathf.Abs(vector.x) , Mathf.Abs(vector.y));
   }
   
   public static Vector2 Absolute(this Vector2 vector)
   {
      return new Vector2(Mathf.Abs(vector.x) , Mathf.Abs(vector.y));
   }
}
