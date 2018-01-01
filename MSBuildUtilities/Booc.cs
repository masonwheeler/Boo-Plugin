﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Hill30.Boo.MSBuildUtilities
{
    public class Booc : ToolTask
    {
        #region Task properties

        public string[] AdditionalLibPaths { get; set; }

        /// <summary>
        /// Allows to compile unsafe code.
        /// </summary>
        public bool AllowUnsafeBlocks { get; set; }

        private bool? checkForOverflowUnderflow;

        /// <summary>
        /// Gets/sets if integer overlow checking is enabled.
        /// </summary>
        public bool CheckForOverflowUnderflow
        {
            get { return checkForOverflowUnderflow ?? true; }
            set { checkForOverflowUnderflow = value; }
        }

        /// <summary>
        /// Gets/sets the culture.
        /// </summary>
        public string Culture { get; set; }

        /// <summary>
        /// Gets/sets the conditional compilation symbols.
        /// </summary>
        public string DefineSymbols { get; set; }

        public bool DelaySign { get; set; }

        /// <summary>
        /// Gets/sets a comma-separated list of warnings that should be disabled.
        /// </summary>
        public string DisabledWarnings { get; set; }

        /// <summary>
        /// Gets/sets if we want to use ducky mode.
        /// </summary>
        public bool Ducky { get; set; }

        public bool EmitDebugInformation { get; set; }

        /// <summary>
        /// If set to true the task will output warnings and errors with full file paths
        /// </summary>
        public bool GenerateFullPaths { get; set; }

        public string KeyContainer { get; set; }

        public string KeyFile { get; set; }

        public bool NoConfig { get; set; }

        public bool NoLogo { get; set; }

        /// <summary>
        /// Gets/sets if we want to link to the standard libraries or not.
        /// </summary>
        public bool NoStandardLib { get; set; }

        /// <summary>
        /// Gets/sets a comma-separated list of optional warnings that should be enabled.
        /// </summary>
        public string OptionalWarnings { get; set; }

        [Output]
        public ITaskItem OutputAssembly { get; set; }

        private string pipeline;

        /// <summary>
        /// Gets/sets a specific pipeline to add to the compiler process.
        /// </summary>
        public string Pipeline
        {
            get 
            { 
                return pipeline ?? AssemblyName + ".PipeLine" + ", " + AssemblyName + ", Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; 
            }
            set { pipeline = value; }
        }

        /// <summary>
        ///Specifies target platform.
        /// </summary>
        public string Platform { get; set; }

        [Required]
        public ITaskItem[] References { get; set; }

        [Required]
        public ITaskItem[] ResponseFiles { get; set; }

        [Required]
        public ITaskItem[] Resources { get; set; }

        [Required]
        public ITaskItem[] Sources { get; set; }

        /// <summary>
        /// Gets/sets the source directory.
        /// </summary>
        public string SourceDirectory { get; set; }

        /// <summary>
        /// Gets/sets whether strict mode is enabled.
        /// </summary>
        public bool Strict { get; set; }

        public string TargetType { get; set; }

        public string TargetFrameworkVersion { get; set; }

        public bool TreatWarningsAsErrors { get; set; }

        public bool Utf8Output { get; set; }

        /// <summary>
        /// Gets/sets the verbosity level.
        /// </summary>
        public string Verbosity { get; set; }

        /// <summary>
        /// Gets/sets a comma-separated list of warnings that should be treated as errors.
        /// </summary>
        public string WarningsAsErrors { get; set; }

        /// <summary>
        /// Gets/sets if we want to use whitespace agnostic mode.
        /// </summary>
        public bool WhiteSpaceAgnostic { get; set; }

        #endregion

        protected override string GenerateFullPathToTool()
        {
            return Path.Combine(Path.GetDirectoryName(GetType().Assembly.CodeBase.Substring(8)), ToolName);
        }

        protected override string ToolName
        {
            get { return AssemblyName + ".exe"; }
        }

		private readonly Dictionary<string, string> _assemblyNames = new Dictionary<string, string>
		{
			{"v4.5", "boocNET45"},
			{"v4.5.1", "boocNET451"},
			{"v4.5.2", "boocNET452"},
			{"v4.6", "boocNET46"},
			{"v4.6.1", "boocNET461"},
		};

        private string AssemblyName
        {
	        get
	        {
		        string result;
		        if (_assemblyNames.TryGetValue(TargetFrameworkVersion, out result))
			        return result;
		        throw new ArgumentException(string.Format("Unknown runtime version {0}.", TargetFrameworkVersion));
	        }
        }

        protected override string GenerateCommandLineCommands()
        {
            var commandLine = new CommandLineBuilder();

			//commandLine.AppendSwitch("-p:\"delay\"");

			commandLine.AppendSwitchIfNotNull("-t:", TargetType.ToLower());
            commandLine.AppendSwitchIfNotNull("-o:", OutputAssembly);
            commandLine.AppendSwitchIfNotNull("-c:", Culture);
            commandLine.AppendSwitchIfNotNull("-srcdir:", SourceDirectory);
            commandLine.AppendSwitchIfNotNull("-keyfile:", KeyFile);
            commandLine.AppendSwitchIfNotNull("-keycontainer:", KeyContainer);
            commandLine.AppendSwitchIfNotNull("-p:", Pipeline);
            commandLine.AppendSwitchIfNotNull("-define:", DefineSymbols);
            commandLine.AppendSwitchIfNotNull("-lib:", AdditionalLibPaths, ",");
            commandLine.AppendSwitchIfNotNull("-nowarn:", DisabledWarnings);
            commandLine.AppendSwitchIfNotNull("-warn:", OptionalWarnings);
            commandLine.AppendSwitchIfNotNull("-platform:", Platform);
		
		    if (TreatWarningsAsErrors)
			    commandLine.AppendSwitch("-warnaserror"); // all warnings are errors
		    else
			    commandLine.AppendSwitchIfNotNull("-warnaserror:", WarningsAsErrors); // only specific warnings are errors
		
		    if (NoLogo)
		        commandLine.AppendSwitch("-nologo");

		    if (NoConfig)
		        commandLine.AppendSwitch("-noconfig");

		    if (NoStandardLib)
		        commandLine.AppendSwitch("-nostdlib");

		    if (DelaySign)
		        commandLine.AppendSwitch("-delaysign");

		    if (WhiteSpaceAgnostic)
		        commandLine.AppendSwitch("-wsa");

		    if (Ducky)
		        commandLine.AppendSwitch("-ducky");

		    if (Utf8Output)
		        commandLine.AppendSwitch("-utf8");

		    if (Strict)
		        commandLine.AppendSwitch("-strict");

		    if (AllowUnsafeBlocks)
		        commandLine.AppendSwitch("-unsafe");

            commandLine.AppendSwitch(EmitDebugInformation ? "-debug+" : "-debug-");

            commandLine.AppendSwitch(CheckForOverflowUnderflow ? "-checked+" : "-checked-");

		    foreach (var rsp in ResponseFiles)
			    commandLine.AppendSwitchIfNotNull("@", rsp.ItemSpec);				

		    foreach (var reference in References)
			    commandLine.AppendSwitchIfNotNull("-r:", reference.ItemSpec);
				
		    foreach (var resource in Resources)
                switch (resource.GetMetadata("Type"))
                {
                    case "Resx":
                        commandLine.AppendSwitchIfNotNull("-resource:", resource.ItemSpec + "," + resource.GetMetadata("LogicalName"));
                        break;
                    case "Non-Resx":
                        commandLine.AppendSwitchIfNotNull("-embedres:", resource.ItemSpec + "," + resource.GetMetadata("LogicalName"));
                        break;
                }
		
		    if (!string.IsNullOrEmpty(Verbosity) )
                switch (Verbosity.ToLower())
                {
                    case "normal":
                        break;
                    case "warning":
                        commandLine.AppendSwitch("-v");
                        break;
                    case "info":
                        commandLine.AppendSwitch("-vv");
                        break;
                    case "verbose":
                        commandLine.AppendSwitch("-vvv");
                        break;
                    default:
                        Log.LogErrorWithCodeFromResources(
                            "Vbc.EnumParameterHasInvalidValue",
                            "Verbosity",
                            Verbosity,
                            "Normal, Warning, Info, Verbose");
                        break;
                }

            commandLine.AppendFileNamesIfNotNull(Sources, " ");

            return commandLine.ToString();
        }

        /// <summary>
        /// Captures the file, line, column, code, and message from a BOO warning
        /// in the form of: Program.boo(1,1): BCW0000: WARNING: This is a warning.
        /// </summary>
        private readonly Regex warningPattern = 
            new Regex(
                "^(?<file>.*?)(\\((?<line>\\d+),(?<column>\\d+)\\):)?" +
                "(\\s?)(?<code>BCW\\d{4}):(\\s)WARNING:(\\s)(?<message>.*)$",
                RegexOptions.Compiled);

        /// <summary>
        /// Captures the file, line, column, code, error type, and message from a
        /// BOO error of the form of:
        /// 1. Program.boo(1,1): BCE0000: This is an error.
        /// 2. Program.boo(1,1): BCE0000: Boo.Lang.Compiler.CompilerError:
        ///            This is an error. ---> Program.boo:4:19: This is an error
        /// 3. BCE0000: This is an error.
        /// 4. Fatal error: This is an error.
        ///
        ///  The second line of the following error format is not cought because 
        /// .NET does not support if|then|else in regular expressions,
        ///  and the regex will be horrible complicated.  
        ///  The second line is as worthless as the first line.
        ///  Therefore, it is not worth implementing it.
        ///
        ///            Fatal error: This is an error.
        ///            Parameter name: format.
        /// </summary>
        private readonly Regex errorPattern =
            new Regex(
                "^(((?<file>.*?)\\((?<line>\\d+),(?<column>\\d+)\\): )?" +
                "(?<code>BCE\\d{4})|(?<errorType>Fatal) error):" +
                "( Boo.Lang.Compiler.CompilerError:)?" +
                " (?<message>.*?)($| --->)",
                RegexOptions.Compiled |
                RegexOptions.ExplicitCapture |
                RegexOptions.Multiline);

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            switch (messageImportance)
            {
                case MessageImportance.Normal:
                    var warningPatternMatch = warningPattern.Match(singleLine);
                    var errorPatternMatch = errorPattern.Match(singleLine);
                    if (warningPatternMatch.Success)
                    {
                        int lineNumber;
                        if (!int.TryParse(warningPatternMatch.Groups["line"].Value, out lineNumber))
                            lineNumber = 0;
                        int columnNumber;
                        if (!int.TryParse(warningPatternMatch.Groups["column"].Value, out columnNumber))
                            columnNumber = 0;
                        Log.LogWarning(
                            null,
                            warningPatternMatch.Groups["code"].Value,
                            null,
                            warningPatternMatch.Groups["file"].Value,
                            lineNumber,
                            columnNumber,
                            0,
                            0,
                            warningPatternMatch.Groups["message"].Value
                            );
                    }
                    else if (errorPatternMatch.Success)
                    {
                        var code = errorPatternMatch.Groups["code"].Value;
                        if (string.IsNullOrEmpty(code))
                            code = "BCE0000";
                        var file = errorPatternMatch.Groups["file"].Value;
                        if (string.IsNullOrEmpty(file))
                            file = "BOOC";
                        int lineNumber;
                        if (!int.TryParse(errorPatternMatch.Groups["line"].Value, out lineNumber))
                            lineNumber = 0;
                        int columnNumber;
                        if (!int.TryParse(errorPatternMatch.Groups["column"].Value, out columnNumber))
                            columnNumber = 0;
                        Log.LogError(
                            errorPatternMatch.Groups["errorType"].Value.ToLower(),
                            code,
                            null,
                            file,
                            lineNumber,
                            columnNumber,
                            0,
                            0,
                            errorPatternMatch.Groups["message"].Value
                            );
                    }
                    break;
            }
            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }

        //override 
    }
}
