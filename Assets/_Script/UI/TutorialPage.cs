using System;
using UnityEngine;

/// <summary>
/// Data structure representing a single page of the tutorial pop-up.
/// </summary>
[Serializable]
public class TutorialPage
{
    [Tooltip("Optional image illustration for this tutorial page. Leave empty if no image is needed.")]
    public Sprite pageImage;

    [Tooltip("Tutorial instruction text for this page.")]
    [TextArea(3, 6)]
    public string pageDescription;
}
