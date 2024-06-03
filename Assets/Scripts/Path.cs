using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path<T>
{
  public List<T> route;
  public float totalCost;

  public Path()
  {
    route = new List<T>();
  }

  public Path(List<T> route, float cost)
  {
    this.route = route;
    totalCost = cost;
  }
}
