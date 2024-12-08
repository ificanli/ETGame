using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DamageNumbersPro
{
    [System.Serializable]
    public struct DigitSettings
    {
        public DigitSettings(float customDefault)
        {
            decimals = 0;
            decimalChar = ".";
            hideZeros = false;

            dotSeparation = false;
            dotDistance = 3;
            dotChar = ".";

            suffixShorten = false;
            suffixes = new List<string>() { "K", "M", "B" };
            suffixDigits = new List<int>() { 3, 3, 3 };
            maxDigits = 4;
        }

        [Header("小数:")]
        [Range(0,3)]
        [Tooltip("点后可见的数字数。")]
        public int decimals;
        [Tooltip("用于点的字符。")]
        public string decimalChar;
        [Tooltip("如果为真，数字末尾的零将被隐藏。")]
        public bool hideZeros;

        [Header("点:")]
        [Tooltip("用点分隔数字。")]
        public bool dotSeparation;
        [Tooltip("每个点之间的位数。")]
        public int dotDistance;
        [Tooltip("用于点的字符。")]
        public string dotChar;

        [Header("后缀缩短:")]
        [Tooltip("将10000这样的数字缩短为10K。")]
        public bool suffixShorten;
        [Tooltip("后缀列表。")]
        public List<string> suffixes;
        [Tooltip("相应的一个后缀缩短了多少个数字的列表。保持两个列表的大小相同。")]
        public List<int> suffixDigits;
        [Tooltip("可见数字的最大值。如果number有更多的数字，它将被缩短。")]
        public int maxDigits;
    }
}