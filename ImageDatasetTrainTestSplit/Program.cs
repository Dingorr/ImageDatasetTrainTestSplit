using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace ImageDatasetTrainTestSplit
{
    class Program
    {
        private static readonly Dictionary<string, List<string>> _classImageDictionary = new Dictionary<string, List<string>>();
        private static readonly List<string> _classesFriendlyName = new List<string>();
        private static string[] _classesDirectory;
        private static string _trainingSetDirectory;
        private static readonly string _testSetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_set");
        private static readonly string _validationSetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "validation_set");
        private static readonly string _newTrainingSetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "new_training_set");
        private static bool _renameFiles = false;

        static void Main(string[] args)
        {
            bool validInput = false;
            string trainDirectory = string.Empty;

            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("DISCLAIMER: THIS PROGRAM SHOULD BE RUN WITH ONLY A TRAINING SET, SINCE IT WILL TRUNCATE ANY EXISTING TEST AND VALIDATION FOLDER.");
            Console.WriteLine("------------------------------------------------------");

            Console.WriteLine("Choose folder with training set (should be in same directory as this file):");
            var directories = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < directories.Length; i++)
            {
                string directory = directories[i];
                Console.WriteLine($"[{i + 1}]: {directory}");
            }

            while (!validInput)
            {
                Console.WriteLine("Type in number for directory:");
                string choice = Console.ReadLine();
                if (int.TryParse(choice, out int choiceInt))
                {
                    int directoryIndex = choiceInt - 1;
                    if (directoryIndex < 0 || directoryIndex > directories.Length)
                    {
                        Console.WriteLine("Invalid choice");
                        continue;
                    }

                    trainDirectory = directories[directoryIndex];
                    validInput = true;
                }
                else
                {
                    Console.WriteLine("Invalid choice");
                }
            }

            _trainingSetDirectory = trainDirectory;
            _classesDirectory = Directory.GetDirectories(trainDirectory);

            foreach (var classDir in _classesDirectory)
            {
                string friendlyName = classDir.Substring(classDir.LastIndexOf("\\") + 1);
                _classesFriendlyName.Add(friendlyName);
                _classImageDictionary.Add(friendlyName, new List<string>());
                foreach (string fileName in Directory.GetFiles(classDir))
                {
                    _classImageDictionary[friendlyName].Add(fileName);
                }
                var random = new Random();
                _classImageDictionary[friendlyName] =_classImageDictionary[friendlyName].OrderBy(x => random.Next()).ToList(); //shuffle list so it is already random
            }

            Console.WriteLine($"Found {_classesDirectory.Length} classes:");
            foreach (var aClass in _classesFriendlyName)
            {
                Console.WriteLine($"\t{aClass} ({_classImageDictionary[aClass].Count} images)");
            }

            Console.WriteLine($"\t{_classImageDictionary.Sum(x => x.Value.Count)} images in total");

            Console.WriteLine("Rename files to be in format {class_name}.{index}.{file_extension}, eg. dangerous.1.jpg? (Y/N)");
            validInput = false;
            while (!validInput)
            {
                var key = Console.ReadKey();
                Console.WriteLine();
                if (key.Key == ConsoleKey.Y)
                {
                    validInput = true;
                    _renameFiles = true;
                }
                else if (key.Key != ConsoleKey.N)
                {
                    Console.WriteLine("Invalid key");
                }
            }

            bool onlySplitToTest = false; //Determines if training set is split into test set or also cross validation set
            validInput = false;

            while (!validInput)
            {
                Console.WriteLine("Split into test set or test set and cross validation set?");
                Console.WriteLine("[1]: Training and test set split");
                Console.WriteLine("[2]: Training, test and cross validation set split");
                string choice = Console.ReadLine();
                if (choice != "1" && choice != "2")
                {
                    Console.WriteLine("Invalid choice");
                    continue;
                }

                onlySplitToTest = choice == "1";
                validInput = true;
            }

            EnsureFolderStructureIsCorrect(_newTrainingSetDirectory);

            if(onlySplitToTest)
                HandleTestSplit();
            else
                HandleTestAndValidationSplit();

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        static void HandleTestSplit()
        {
            bool validInput = false;
            float percentage = 0f;

            while (!validInput)
            {
                Console.WriteLine("Choose percentage to use af test set (written as decimal):");
                string choice = Console.ReadLine();
                if (float.TryParse(choice, NumberStyles.Float, CultureInfo.InvariantCulture, out percentage))
                {
                    if (percentage < 0 || percentage > 1)
                    {
                        Console.WriteLine("Percentage needs to be between 0 and 1");
                        continue;
                    }

                    validInput = true;
                    Console.WriteLine($"Percentage chosen: {percentage * 100}%");
                }
                else
                {
                    Console.WriteLine("Invalid input");
                }
            }

            EnsureFolderStructureIsCorrect(_testSetDirectory);

            foreach (var className in _classesFriendlyName)
            {
                int testFilesToTake = (int) Math.Round(((float)_classImageDictionary[className].Count) * percentage);
                int trainingFilesToTake = _classImageDictionary[className].Count - testFilesToTake;
                var testFileNames = _classImageDictionary[className].Take(testFilesToTake).ToList();
                var trainingFileNames = _classImageDictionary[className].TakeLast(trainingFilesToTake).ToList();

                Console.WriteLine($"[{className}]: Taking {testFilesToTake} to test set, and {trainingFilesToTake} to training set out of {_classImageDictionary[className].Count}");
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();

                int[] fileNumbers = { 1, 1 };

                foreach (string fileName in _classImageDictionary[className])
                {
                    string fileFriendlyName = fileName.Substring(fileName.LastIndexOf("\\") + 1);
                    string extension = fileFriendlyName.Substring(fileFriendlyName.LastIndexOf('.') + 1);

                    if (_renameFiles)
                    {
                        if (testFileNames.Contains(fileName))
                        {
                            string newFileName = $"{className}.{fileNumbers[0]++}.{extension}";
                            string pathAndFileName = Path.Combine(_testSetDirectory, className, newFileName);
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_testSetDirectory, className)} as {newFileName}");
                            File.Copy(fileName, pathAndFileName, true);
                        }
                        else
                        {
                            string newFileName = $"{className}.{fileNumbers[1]++}.{extension}";
                            string pathAndFileName = Path.Combine(_newTrainingSetDirectory, className, newFileName);
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_newTrainingSetDirectory, className)} as {newFileName}");
                            File.Copy(fileName, pathAndFileName, true);
                        }
                    }
                    else
                    {
                        if (testFileNames.Contains(fileName))
                        {
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_testSetDirectory, className)}");
                            File.Copy(fileName, Path.Combine(_testSetDirectory, className, fileFriendlyName), true);
                        }
                        else
                        {
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_newTrainingSetDirectory, className)}");
                            File.Copy(fileName, Path.Combine(_newTrainingSetDirectory, className, fileFriendlyName), true);
                        }
                    }
                }
            }
        }

        static void HandleTestAndValidationSplit()
        {
            bool validInput = false;
            float testPercentage = 0f;
            float validationPercentage = 0f;

            while (!validInput)
            {
                Console.WriteLine("Choose percentage to use af test set (written as decimal):");
                string choice = Console.ReadLine();
                if (float.TryParse(choice, NumberStyles.Float, CultureInfo.InvariantCulture, out testPercentage))
                {
                    if (testPercentage < 0 || testPercentage > 1)
                    {
                        Console.WriteLine("Percentage needs to be between 0 and 1");
                        continue;
                    }

                    validInput = true;
                }
                else
                {
                    Console.WriteLine("Invalid input");
                }
            }

            validInput = false;
            while (!validInput)
            {
                Console.WriteLine("Choose percentage to use af validation set (written as decimal):");
                string choice = Console.ReadLine();
                if (float.TryParse(choice, NumberStyles.Float, CultureInfo.InvariantCulture, out validationPercentage))
                {
                    if (validationPercentage < 0 || validationPercentage > 1)
                    {
                        Console.WriteLine("Percentage needs to be between 0 and 1");
                        continue;
                    }
                    else if (validationPercentage + testPercentage > 1)
                    {
                        Console.WriteLine("Validation and test split has to sum up to a maximum of 1 (100%)");
                        continue;
                    }

                    validInput = true;
                }
                else
                {
                    Console.WriteLine("Invalid input");
                }
            }

            Console.WriteLine($"Training set split delegation: {testPercentage * 100}% test, {validationPercentage * 100}% validation, {(1f - (testPercentage + validationPercentage)) * 100}% training");

            EnsureFolderStructureIsCorrect(_testSetDirectory);
            EnsureFolderStructureIsCorrect(_validationSetDirectory);

            foreach (var className in _classesFriendlyName)
            {
                int testFilesToTake = (int)Math.Round(((float)_classImageDictionary[className].Count) * testPercentage);
                int validationFilesToTake = (int)Math.Round(((float)_classImageDictionary[className].Count) * validationPercentage);
                int trainingFilesToTake = _classImageDictionary[className].Count - testFilesToTake - validationFilesToTake;
                var testFileNames = _classImageDictionary[className].Take(testFilesToTake).ToList();
                var validationFileNames = _classImageDictionary[className].Where((fn, index) =>
                    index >= testFilesToTake && index < testFilesToTake + validationFilesToTake).ToList();
                var trainingFileNames = _classImageDictionary[className].TakeLast(trainingFilesToTake).ToList();

                Console.WriteLine($"[{className}]: Taking {testFileNames.Count} to test set, {validationFileNames.Count} to validation set and {trainingFileNames.Count} to training set out of {_classImageDictionary[className].Count}");
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();

                int[] fileNumbers = new int[3] { 1, 1, 1 };

                foreach (string fileName in _classImageDictionary[className])
                {
                    string fileFriendlyName = fileName.Substring(fileName.LastIndexOf("\\") + 1);
                    string extension = fileFriendlyName.Substring(fileFriendlyName.LastIndexOf('.') + 1);

                    if (_renameFiles)
                    {
                        if (testFileNames.Contains(fileName))
                        {
                            string newFileName = $"{className}.{fileNumbers[0]++}.{extension}";
                            string pathAndFileName = Path.Combine(_testSetDirectory, className, newFileName);
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_testSetDirectory, className)} as {newFileName}");
                            File.Copy(fileName, pathAndFileName, true);
                        }
                        else if (validationFileNames.Contains(fileName))
                        {
                            string newFileName = $"{className}.{fileNumbers[1]++}.{extension}";
                            string pathAndFileName = Path.Combine(_validationSetDirectory, className, newFileName);
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_validationSetDirectory, className)} as {newFileName}");
                            File.Copy(fileName, pathAndFileName, true);
                        }
                        else
                        {
                            string newFileName = $"{className}.{fileNumbers[2]++}.{extension}";
                            string pathAndFileName = Path.Combine(_newTrainingSetDirectory, className, newFileName);
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_newTrainingSetDirectory, className)} as {newFileName}");
                            File.Copy(fileName, pathAndFileName, true);
                        }
                    }
                    else
                    {
                        if (testFileNames.Contains(fileName))
                        {
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_testSetDirectory, className)}");
                            File.Copy(fileName, Path.Combine(_testSetDirectory, className, fileFriendlyName), true);
                        }
                        else if (validationFileNames.Contains(fileName))
                        {
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_validationSetDirectory, className)}");
                            File.Copy(fileName, Path.Combine(_validationSetDirectory, className, fileFriendlyName), true);
                        }
                        else
                        {
                            Console.WriteLine($"Copying file {fileFriendlyName} to {Path.Combine(_newTrainingSetDirectory, className)}");
                            File.Copy(fileName, Path.Combine(_newTrainingSetDirectory, className, fileFriendlyName), true);
                        }
                    }
                }
            }
        }

        static void EnsureFolderStructureIsCorrect(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                foreach (var className in _classesFriendlyName)
                {
                    Directory.CreateDirectory(Path.Combine(path, className));
                }
            }
            else
            {
                var directoryInfo = new DirectoryInfo(path);
                bool containsFiles = false;
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    if (directory.GetFiles().Length > 0)
                    {
                        containsFiles = true;
                        break;
                    }
                }

                if (containsFiles)
                {
                    Console.WriteLine($"Found files in folder or sub-folder of {path} which will be deleted");
                    Directory.Delete(path, true);
                    Directory.CreateDirectory(path);
                    foreach (var className in _classesFriendlyName)
                    {
                        Directory.CreateDirectory(Path.Combine(path, className));
                    }
                }
                else
                {
                    foreach (var className in _classesFriendlyName)
                    {
                        if (!Directory.Exists(Path.Combine(path, className)))
                        {
                            Directory.CreateDirectory(Path.Combine(path, className));
                        }
                    }
                }
            }
        }
    }
}
