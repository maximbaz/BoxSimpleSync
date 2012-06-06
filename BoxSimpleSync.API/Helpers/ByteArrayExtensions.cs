using System;

namespace BoxSimpleSync.API.Helpers
{
    public static class ByteArrayExtensions
    {
        #region Public and Internal Methods

        public static byte[] Merge(this byte[] first, byte[] second) {
            var result = new byte[first.Length + second.Length];

            Array.Copy(first, result, first.Length);
            Array.Copy(second, 0, result, first.Length, second.Length);

            return result;
        }

        #endregion
    }
}