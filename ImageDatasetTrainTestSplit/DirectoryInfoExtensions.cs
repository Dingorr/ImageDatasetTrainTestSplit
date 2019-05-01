using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ImageDatasetTrainTestSplit
{
    static class DirectoryInfoExtensions
    {
        public static FileInfo[] GetFilesFilters(this DirectoryInfo directoryInfo, string[] filters)
        {
            var fileInfos = new List<FileInfo>();
            foreach (string filter in filters)
            {
                fileInfos.AddRange(directoryInfo.GetFiles(filter));
            }

            return fileInfos.ToArray();
        }
    }
}
