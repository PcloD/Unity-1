using UnityEngine;

public static class UILabelExtension
{
  public static void SafeText(this UILabel self, string value)
  {
    if (self != null)
    {
      self.text = value;
    }
  }
}
