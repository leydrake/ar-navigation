using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SetUiScript : MonoBehaviour
{
    [SerializeField]
    private TMP_Text textField;
    
    [SerializeField]
    private string fixedText;

    public void OnSliderValueChanged(float numericValue) {
        textField.text = $"{fixedText}: {numericValue}";
    }
}
