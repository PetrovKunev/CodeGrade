using CodeGrade.Models;
using System.Text.RegularExpressions;

namespace CodeGrade.Services
{
    public class CodeQualityAnalyzer
    {
        private readonly ILogger<CodeQualityAnalyzer> _logger;

        public CodeQualityAnalyzer(ILogger<CodeQualityAnalyzer> logger)
        {
            _logger = logger;
        }

        public async Task<QualityMetrics> AnalyzeCodeQualityAsync(string code, string language)
        {
            var metrics = new QualityMetrics
            {
                LinesOfCode = CalculateLinesOfCode(code),
                CyclomaticComplexity = CalculateComplexity(code),
                ReadabilityScore = AnalyzeReadability(code, language),
                NamingScore = AnalyzeNaming(code, language),
                StructureScore = AnalyzeStructure(code, language),
                DocumentationCoverage = AnalyzeDocumentation(code, language),
                StyleScore = AnalyzeStyle(code, language),
                MaintainabilityIndex = CalculateMaintainabilityIndex(code),
                AnalysisVersion = "1.0",
                ToolsUsed = "CodeGrade Static Analyzer"
            };

            // Calculate composite scores
            metrics.MethodCount = CountMethods(code, language);
            metrics.ClassCount = CountClasses(code, language);
            metrics.StyleViolations = CountStyleViolations(code, language);
            
            // Generate quality feedback
            metrics.QualityFeedback = GenerateQualityFeedback(metrics);
            metrics.ImprovementSuggestions = GenerateImprovementSuggestions(metrics, code, language);

            return metrics;
        }

        public int CalculateComplexity(string code)
        {
            // Basic cyclomatic complexity calculation
            var complexityKeywords = new[] { "if", "else", "while", "for", "foreach", "switch", "case", "catch", "&&", "||" };
            var complexity = 1; // Base complexity

            foreach (var keyword in complexityKeywords)
            {
                complexity += Regex.Matches(code, $@"\b{keyword}\b", RegexOptions.IgnoreCase).Count;
            }

            return complexity;
        }

        private int CalculateLinesOfCode(string code)
        {
            return code.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Count(line => !string.IsNullOrWhiteSpace(line.Trim()) && !line.Trim().StartsWith("//"));
        }

        private int AnalyzeReadability(string code, string language)
        {
            int score = 100;

            // Check average line length
            var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var avgLineLength = lines.Average(line => line.Length);
            if (avgLineLength > 120) score -= 20;
            else if (avgLineLength > 80) score -= 10;

            // Check indentation consistency
            var indentationPattern = GetIndentationPattern(code);
            if (!IsConsistentIndentation(lines, indentationPattern)) score -= 15;

            // Check for meaningful whitespace
            if (!HasProperSpacing(code)) score -= 10;

            // Check for nested depth
            var maxNesting = CalculateMaxNestingDepth(code);
            if (maxNesting > 4) score -= 15;
            else if (maxNesting > 3) score -= 10;

            return Math.Max(0, score);
        }

        private int AnalyzeNaming(string code, string language)
        {
            int score = 100;

            // Variable naming patterns by language
            var patterns = GetNamingPatterns(language);
            
            // Check variable names
            var variableMatches = Regex.Matches(code, patterns.VariablePattern);
            var goodVariableNames = 0;
            foreach (Match match in variableMatches)
            {
                var varName = match.Groups[1].Value;
                if (IsGoodVariableName(varName)) goodVariableNames++;
            }

            if (variableMatches.Count > 0)
            {
                var namingRatio = (double)goodVariableNames / variableMatches.Count;
                score = (int)(score * namingRatio);
            }

            // Check for single letter variables (except loop counters)
            var singleLetterVars = Regex.Matches(code, @"\b[a-z]\b(?!\s*=\s*\d)");
            score -= singleLetterVars.Count * 5;

            return Math.Max(0, score);
        }

        private int AnalyzeStructure(string code, string language)
        {
            int score = 100;

            // Check method length
            var methods = ExtractMethods(code, language);
            foreach (var method in methods)
            {
                var methodLines = method.Split('\n').Length;
                if (methodLines > 50) score -= 15;
                else if (methodLines > 30) score -= 10;
            }

            // Check for proper separation of concerns
            var classCount = CountClasses(code, language);
            var methodCount = CountMethods(code, language);
            
            if (classCount > 0 && methodCount / classCount > 10) score -= 20; // Too many methods per class

            // Check for code duplication
            if (HasCodeDuplication(code)) score -= 15;

            return Math.Max(0, score);
        }

        private int AnalyzeDocumentation(string code, string language)
        {
            var documentationPatterns = GetDocumentationPatterns(language);
            var totalMethods = CountMethods(code, language);
            var totalClasses = CountClasses(code, language);
            
            if (totalMethods + totalClasses == 0) return 100;

            var documentedMethods = 0;
            var documentedClasses = 0;

            // Count documented methods and classes
            foreach (var pattern in documentationPatterns.MethodPatterns)
            {
                documentedMethods += Regex.Matches(code, pattern).Count;
            }

            foreach (var pattern in documentationPatterns.ClassPatterns)
            {
                documentedClasses += Regex.Matches(code, pattern).Count;
            }

            var totalDocumentable = totalMethods + totalClasses;
            var totalDocumented = Math.Min(documentedMethods, totalMethods) + Math.Min(documentedClasses, totalClasses);

            return (int)Math.Round((double)totalDocumented / totalDocumentable * 100);
        }

        private int AnalyzeStyle(string code, string language)
        {
            int score = 100;
            var violations = CountStyleViolations(code, language);
            
            // Deduct points for style violations
            score -= violations * 2;

            return Math.Max(0, score);
        }

        private int CalculateMaintainabilityIndex(string code)
        {
            // Simplified maintainability index calculation
            var loc = CalculateLinesOfCode(code);
            var complexity = CalculateComplexity(code);
            
            // Basic formula (simplified version of Halstead + Cyclomatic complexity)
            var maintainabilityIndex = Math.Max(0, 171 - 5.2 * Math.Log(loc) - 0.23 * complexity);
            
            return (int)Math.Min(100, maintainabilityIndex);
        }

        private int CountMethods(string code, string language)
        {
            var patterns = GetMethodPatterns(language);
            return patterns.Sum(pattern => Regex.Matches(code, pattern).Count);
        }

        private int CountClasses(string code, string language)
        {
            var patterns = GetClassPatterns(language);
            return patterns.Sum(pattern => Regex.Matches(code, pattern).Count);
        }

        private int CountStyleViolations(string code, string language)
        {
            int violations = 0;
            var rules = GetStyleRules(language);

            foreach (var rule in rules)
            {
                violations += Regex.Matches(code, rule.Pattern).Count;
            }

            return violations;
        }

        private string GenerateQualityFeedback(QualityMetrics metrics)
        {
            var feedback = new List<string>();

            if (metrics.ReadabilityScore < 70)
                feedback.Add("Improve code readability with better formatting and naming.");
            
            if (metrics.CyclomaticComplexity > 10)
                feedback.Add("Consider reducing complexity by breaking down large methods.");
            
            if (metrics.DocumentationCoverage < 50)
                feedback.Add("Add more comments and documentation to explain your code.");
            
            if (metrics.StyleScore < 80)
                feedback.Add("Follow language-specific style conventions more consistently.");

            if (feedback.Count == 0)
                feedback.Add("Good code quality overall! Keep up the excellent work.");

            return string.Join(" ", feedback);
        }

        private string GenerateImprovementSuggestions(QualityMetrics metrics, string code, string language)
        {
            var suggestions = new List<string>();

            if (metrics.CyclomaticComplexity > 15)
                suggestions.Add("Break complex methods into smaller, focused functions.");

            if (metrics.LinesOfCode > 200 && metrics.MethodCount < 5)
                suggestions.Add("Consider splitting long code into multiple methods for better organization.");

            if (metrics.NamingScore < 70)
                suggestions.Add("Use more descriptive variable and method names.");

            if (metrics.DocumentationCoverage < 30)
                suggestions.Add("Add header comments to explain what your methods do.");

            return string.Join(" ", suggestions);
        }

        // Helper methods for language-specific analysis
        private NamingPatterns GetNamingPatterns(string language)
        {
            return language.ToLower() switch
            {
                "csharp" => new NamingPatterns 
                { 
                    VariablePattern = @"\b[a-z][a-zA-Z0-9]*\s+([a-zA-Z][a-zA-Z0-9]*)\b",
                    MethodPattern = @"\b[A-Z][a-zA-Z0-9]*\s*\("
                },
                "python" => new NamingPatterns 
                { 
                    VariablePattern = @"\b([a-z][a-z0-9_]*)\s*=",
                    MethodPattern = @"def\s+([a-z][a-z0-9_]*)\s*\("
                },
                "java" => new NamingPatterns 
                { 
                    VariablePattern = @"\b[a-zA-Z]+\s+([a-z][a-zA-Z0-9]*)\b",
                    MethodPattern = @"\b[a-z][a-zA-Z0-9]*\s*\("
                },
                _ => new NamingPatterns 
                { 
                    VariablePattern = @"\b([a-zA-Z][a-zA-Z0-9]*)\b",
                    MethodPattern = @"\b[a-zA-Z][a-zA-Z0-9]*\s*\("
                }
            };
        }

        private string[] GetMethodPatterns(string language)
        {
            return language.ToLower() switch
            {
                "csharp" => new[] { @"\b(public|private|protected|internal).*\s+\w+\s*\([^)]*\)\s*\{" },
                "python" => new[] { @"def\s+\w+\s*\([^)]*\):" },
                "java" => new[] { @"\b(public|private|protected).*\s+\w+\s*\([^)]*\)\s*\{" },
                "javascript" => new[] { @"function\s+\w+\s*\([^)]*\)", @"\w+\s*=\s*\([^)]*\)\s*=>" },
                _ => new[] { @"function\s+\w+\s*\([^)]*\)" }
            };
        }

        private string[] GetClassPatterns(string language)
        {
            return language.ToLower() switch
            {
                "csharp" => new[] { @"\bclass\s+\w+" },
                "python" => new[] { @"class\s+\w+.*:" },
                "java" => new[] { @"\bclass\s+\w+" },
                "javascript" => new[] { @"class\s+\w+" },
                _ => new[] { @"class\s+\w+" }
            };
        }

        private DocumentationPatterns GetDocumentationPatterns(string language)
        {
            return language.ToLower() switch
            {
                "csharp" => new DocumentationPatterns
                {
                    MethodPatterns = new[] { @"///.*\n.*\b(public|private|protected).*\s+\w+\s*\(" },
                    ClassPatterns = new[] { @"///.*\n.*\bclass\s+\w+" }
                },
                "python" => new DocumentationPatterns
                {
                    MethodPatterns = new[] { @"def\s+\w+\s*\([^)]*\):\s*\n\s*""" },
                    ClassPatterns = new[] { @"class\s+\w+.*:\s*\n\s*""" }
                },
                "java" => new DocumentationPatterns
                {
                    MethodPatterns = new[] { @"/\*\*.*?\*/.*\b(public|private|protected).*\s+\w+\s*\(" },
                    ClassPatterns = new[] { @"/\*\*.*?\*/.*\bclass\s+\w+" }
                },
                _ => new DocumentationPatterns
                {
                    MethodPatterns = new[] { @"//.*\n.*function\s+\w+" },
                    ClassPatterns = new[] { @"//.*\n.*class\s+\w+" }
                }
            };
        }

        private StyleRule[] GetStyleRules(string language)
        {
            return language.ToLower() switch
            {
                "csharp" => new[]
                {
                    new StyleRule { Pattern = @"\{\s*\n", Description = "Opening brace should be on same line" },
                    new StyleRule { Pattern = @"\s+$", Description = "Trailing whitespace" }
                },
                "python" => new[]
                {
                    new StyleRule { Pattern = @"\t", Description = "Use spaces instead of tabs" },
                    new StyleRule { Pattern = @"\s+$", Description = "Trailing whitespace" }
                },
                _ => new[]
                {
                    new StyleRule { Pattern = @"\s+$", Description = "Trailing whitespace" }
                }
            };
        }

        // Helper methods
        private bool IsGoodVariableName(string name)
        {
            return name.Length > 2 && !name.All(char.IsDigit) && 
                   !new[] { "temp", "tmp", "var", "obj", "item" }.Contains(name.ToLower());
        }

        private string GetIndentationPattern(string code)
        {
            var firstIndentedLine = code.Split('\n').FirstOrDefault(line => line.StartsWith(" ") || line.StartsWith("\t"));
            return firstIndentedLine?.Substring(0, firstIndentedLine.Length - firstIndentedLine.TrimStart().Length) ?? "    ";
        }

        private bool IsConsistentIndentation(string[] lines, string pattern)
        {
            return lines.Where(line => line.StartsWith(" ") || line.StartsWith("\t"))
                       .All(line => line.StartsWith(pattern) || line.TrimStart().Length == 0);
        }

        private bool HasProperSpacing(string code)
        {
            return !Regex.IsMatch(code, @"[a-zA-Z0-9]\([a-zA-Z0-9]") && // function(param
                   !Regex.IsMatch(code, @"[a-zA-Z0-9]\{[a-zA-Z0-9]") && // if{statement
                   !Regex.IsMatch(code, @"[a-zA-Z0-9]\+[a-zA-Z0-9]");   // var+other
        }

        private int CalculateMaxNestingDepth(string code)
        {
            int maxDepth = 0;
            int currentDepth = 0;
            
            foreach (char c in code)
            {
                if (c == '{' || c == '(' || c == '[')
                {
                    currentDepth++;
                    maxDepth = Math.Max(maxDepth, currentDepth);
                }
                else if (c == '}' || c == ')' || c == ']')
                {
                    currentDepth--;
                }
            }
            
            return maxDepth;
        }

        private string[] ExtractMethods(string code, string language)
        {
            var patterns = GetMethodPatterns(language);
            var methods = new List<string>();
            
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(code, pattern + @".*?(?=\n\s*(?:public|private|protected|class|\Z))", 
                                          RegexOptions.Singleline);
                methods.AddRange(matches.Cast<Match>().Select(m => m.Value));
            }
            
            return methods.ToArray();
        }

        private bool HasCodeDuplication(string code)
        {
            var lines = code.Split('\n').Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line)).ToArray();
            var duplicateLines = lines.GroupBy(line => line).Where(g => g.Count() > 1 && g.Key.Length > 10).Count();
            return duplicateLines > 2;
        }

        // Helper classes
        private class NamingPatterns
        {
            public string VariablePattern { get; set; } = string.Empty;
            public string MethodPattern { get; set; } = string.Empty;
        }

        private class DocumentationPatterns
        {
            public string[] MethodPatterns { get; set; } = Array.Empty<string>();
            public string[] ClassPatterns { get; set; } = Array.Empty<string>();
        }

        private class StyleRule
        {
            public string Pattern { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}