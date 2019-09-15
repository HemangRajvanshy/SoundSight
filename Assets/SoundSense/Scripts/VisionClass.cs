using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class VisionData
{
    public IEnumerable<VisionObject> objects;
}


[Serializable]
public class VisionObject
{
    public float confidence;
    public BoundingRectangle rectangle;
    [JsonProperty(PropertyName = "object")]
    public String obj;
}

[Serializable]
public class BoundingRectangle
{
    public int x;
    public int y;
    public int h;
    public int w;
}