using UnityEngine;

public static class NGUILabelExtension
{
  public static void SafeText(this UILabel self, string value)
  {
    if (self != null)
    {
      self.text = value;
    }
  }
}
