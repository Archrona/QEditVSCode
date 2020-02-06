using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechConsole
{
    static class Language
    {
        public static List<Substitution> subs = null;
        public static bool plainTextMode = true;
        public static string languageInfoString = "";

        public static readonly int low = 0, title = 1, high = 2, concat = 3, snake = 4, hyphen = 5;

        public static int defaultFirstCase = low, defaultCase = title, defaultGlue = concat;

        public static bool debugWords = false;

        public static bool mathMode = false;
        public static bool apostropheIsIdentifier = false;
        public static string[] mathDelimitersSmall = { "$", "$" };
        public static string[] mathDelimitersLarge = { "\\[", "\\]" };




        public static void initialize() {
            plainTextMode = true;
            subs = null;
            languageInfoString = "Plaintext";
        }

        private class LanguageInfo
        {
            public string name;
            public List<string> extensions;
            public string substitutions;
        }

        public static void loadSubstitutions(string extension = "") {

            List<LanguageInfo> languages = JsonConvert.DeserializeObject<List<LanguageInfo>>(File.ReadAllText("languages.json"));
            
            foreach (LanguageInfo info in languages) {
                if (info.extensions.Contains(extension)) {
                    readSubstitutionsFromFile(info.substitutions);
                    languageInfoString = info.name;
                    plainTextMode = false;
                    return;
                }
            }

            // Fallthrough indicates extension not found; enter plaintext mode
            subs = null;
            plainTextMode = true;
            languageInfoString = "Plaintext";

        }

        public static void readSubstitutionsFromFile(string substitutionsFile) { 
            List<Substitution> result = new List<Substitution>();
            string[] lines = File.ReadAllLines(substitutionsFile);
            mathMode = false;
            apostropheIsIdentifier = false;

            List<List<string>> lineWords = new List<List<string>>();
            int lastLine = 0;

            for (int i = 0; i < lines.Length; i++) {
                List<string> words = splitWords(lines[i]);

                if (words.Count == 0) {
                    if (lineWords.Count >= 2) {
                        for (int m = 0; m < lineWords.Count - 1; m++)
                            result.Add(new Substitution(lineWords[m], splitSubstitution(lines[lastLine])));
                    }
                    else if (lineWords.Count == 1) {
                        if (lineWords[0].Count >= 2 && lineWords[0][0] == "$") {
                            if (lineWords[0][1].ToLower() == "math") {
                                mathMode = true;
                            }
                            if (lineWords[0][1].ToLower() == "apostrophe_is_identifier") {
                                apostropheIsIdentifier = true;
                            }
                        }
                    }
                    lineWords.Clear();
                }
                else {
                    lineWords.Add(words);
                    lastLine = i;
                }
            }

            for (int m = 0; m < lineWords.Count - 1; m++)
                result.Add(new Substitution(lineWords[m], splitSubstitution(lines[lastLine])));

            result.Sort((first, second) => -first.matches.Count.CompareTo(second.matches.Count));

            subs = result;
        }

        private static List<string> splitSubstitution(string sub) {
            List<string> result = new List<string>();

            int front = 0, back = 0;

            while (front < sub.Length) {
                char c = sub[front];

                if (isWhite(c)) {
                    front += 1;
                    back = front;
                }
                else {
                    back++;
                    while (back < sub.Length && !isWhite(sub[back])) back++;
                    result.Add(sub.Substring(front, back - front));
                    front = back;
                }
            }

            return result;
        }

        public static bool isAlpha(char c) {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
        }

        public static bool isIdentifier(char c) {
            return c == '$' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || (apostropheIsIdentifier && c == '\'');
        }

        public static bool isNumeric(char c) {
            return (c >= '0' && c <= '9') || c == '.' || c == '-';
        }

        public static bool isWhite(char c) {
            return (c <= 32);
        }

        public static bool isGrouping(char c) {
            return (c == '\"' || c == '\'' || c == '/' || c == '`');
        }

        public static bool isPunctuation(char c) {
            return !isWhite(c) && !isIdentifier(c) && !isGrouping(c);
        }

        public static List<string> splitWords(string code, bool splitPunctuation = true) {
            List<string> result = new List<string>();

            int front = 0, back = 0;

            while (front < code.Length) {
                char c = code[front];

                if (isWhite(c)) {
                    front += 1;
                    back = front;
                }
                else if (isNumeric(c)) {
                    while (back < code.Length && isNumeric(code[back])) back++;
                    result.Add(code.Substring(front, back - front));
                    front = back;
                }
                else if (isIdentifier(c)) {
                    while (back < code.Length && isIdentifier(code[back])) back++;
                    result.Add(code.Substring(front, back - front).ToLower());
                    front = back;
                }
                else if (isGrouping(c)) {
                    back++;
                    while (back < code.Length) {
                        if (code[back] == '\\') back = Math.Min(code.Length, back + 2);
                        else if (code[back] == c) { back++; break; } else back++;
                    }
                    result.Add(code.Substring(front, back - front));
                    front = back;
                }
                else if (isPunctuation(c)) {
                    if (splitPunctuation) {
                        while (back < code.Length && isPunctuation(code[back])) back++;
                        result.Add(code.Substring(front, back - front));
                    }
                    else {
                        result.Add(code.Substring(front, 1));
                        back++;
                    }
                    front = back;
                }
                else {
                    back++;
                    front = back;
                }
            }

            return result;
        }

        public static List<string> makeSubstitutions(List<string> words, out int insertionPoint) {
            List<string> result = new List<string>();
            List<string> toAdd = new List<string>();

            int i = 0;
            insertionPoint = 0;

            // For each word in the spoken input...
            while (i < words.Count) {
                toAdd.Clear();
                bool matchFound = false;

                // Try to find a maximal-length substitution rule.
                for (int s = 0; s < subs.Count; s++) {
                    if (i + subs[s].matches.Count <= words.Count) {
                        bool thisSubMatches = true;

                        for (int k = 0; k < subs[s].matches.Count; k++) {
                            if (subs[s].matches[k] != words[i + k]) {
                                thisSubMatches = false;
                                break;
                            }
                        }

                        if (thisSubMatches) {
                            matchFound = true;
                            toAdd.AddRange(subs[s].replacement);
                            i += subs[s].matches.Count;
                            break;
                        }
                    }
                }

                // If no substitution rule found, read the current token as an identifier or keyword.
                if (!matchFound) {

                    // If the word starts with "$", it is meant literally. "$" by itself is a no-op.
                    if (words[i][0] == '$') {
                        if (words[i].Length > 1) {
                            toAdd.Add(words[i].Substring(1));
                        }
                    }
                    // Otherwise, the word is meant literally. Just add it.
                    else {
                        toAdd.Add(words[i]);
                    }

                    i++;
                }

                // We know what needs inserting now.
                // First remove any "$_" at the insertion point.
                while (insertionPoint < result.Count && result[insertionPoint] == "$_")
                    result.RemoveAt(insertionPoint);

                // Now step through each token we're going to add, adding it at the insertion point.
                // Catch! Some tokens are special, and mean SUPER SPECIAL THINGS.
                //   $blank means, "Put the cursor on the leftmost $_ or the EOS if none exist."
                //   $step means, "If the cursor is not already on a $_, move it to the next $_ or the EOS."
                //   $decline means, "If the cursor is on $_, remove it and then $step."
                //   $zoom means, "Move the cursor to EOS."
                //   $decline_all means, "Remove all $_, then $zoom."
                //   $eol means, "Move the insertion point just before a $line or $EOL."
                //   $start_line means, "If we're not at the start of a line, $decline until EOL and $line."
                //   $_ means, "insert a blank".
                //   $__ means, "insert a blank, and after this is all over put the cursor HERE."
                int setNextInsertionPoint = -1;

                foreach (string token in toAdd) {
                    if (token == "$blank") {
                        insertionPoint = 0;
                        while (insertionPoint < result.Count && result[insertionPoint] != "$_") insertionPoint++;
                    }
                    else if (token == "$step" || token == "$decline") {
                        while (insertionPoint < result.Count && result[insertionPoint] == "$_") result.RemoveAt(insertionPoint);

                        // if we're stepping off a completely empty line, and the previous line was NOT indented, 
                        // remove the empty line BEFORE the insertion point
                        //    [NOT $up or $down] $line >>[$up|$down|nothing] $line   -->  >>[$up|$down|nothing] $line
                        if (insertionPoint > 0 && result[insertionPoint - 1] == "$line") {
                            if (insertionPoint <= 1 || (result[insertionPoint - 2] != "$up" && result[insertionPoint - 2] != "$down")) {
                                if ((insertionPoint < result.Count && result[insertionPoint] == "$line")
                                    || (insertionPoint < result.Count - 1 && result[insertionPoint] == "$up" && result[insertionPoint + 1] == "$line")
                                    || (insertionPoint < result.Count - 1 && result[insertionPoint] == "$down" && result[insertionPoint + 1] == "$line")) {

                                    insertionPoint--;
                                    result.RemoveAt(insertionPoint);
                                }
                            }
                        }

                        while (insertionPoint < result.Count && result[insertionPoint] != "$_") insertionPoint++;

                    }
                    else if (token == "$zoom") {
                        insertionPoint = result.Count;
                    }
                    else if (token == "$decline_all") {
                        result.RemoveAll((x) => x == "$_");
                        insertionPoint = result.Count;
                    }
                    else if (token == "$eol") {
                        while (insertionPoint < result.Count && result[insertionPoint] != "$line") {
                            if (result[insertionPoint] == "$_") {
                                while (insertionPoint < result.Count && result[insertionPoint] == "$_") result.RemoveAt(insertionPoint);
                            }
                            else {
                                insertionPoint++;
                            }
                        }
                        if (insertionPoint > 0 && (result[insertionPoint - 1] == "$down" || result[insertionPoint - 1] == "$up")) {
                            insertionPoint--;
                        }
                    }
                    else if (token == "$start_line") {
                        if (insertionPoint > 0 && result[insertionPoint - 1] != "$line") {
                            while (insertionPoint < result.Count && result[insertionPoint] != "$line") {
                                if (result[insertionPoint] == "$_") {
                                    while (insertionPoint < result.Count && result[insertionPoint] == "$_") result.RemoveAt(insertionPoint);
                                }
                                else {
                                    insertionPoint++;
                                }
                            }

                            // right now, insertion point is either on $line or EOF

                            // if we're at EOF, insert a $line
                            if (insertionPoint == result.Count) {
                                result.Insert(insertionPoint, "$line");
                                insertionPoint++;
                            }

                            // we're on $line.
                            else {
                                // check to see if previous token is $down. if so, step back one and insert line there.
                                //   $down >>$line --->  $line >>$down $line
                                if (insertionPoint > 0 && result[insertionPoint - 1] == "$down") {
                                    insertionPoint--;
                                    result.Insert(insertionPoint, "$line");
                                    insertionPoint++;
                                }

                                // check to see if we've got an open brace, indent, blank combo.
                                // if so just put the cursor on the $_.
                                //   $up >>$line $_  -->  $up $line >>$_
                                else if (insertionPoint < result.Count - 1 && result[insertionPoint + 1] == "$_") {
                                    insertionPoint++;
                                }

                                // otherwise, just insert a $line and step right after that.
                                //   >>$line  -->  $line >>$line
                                else {
                                    result.Insert(insertionPoint, "$line");
                                    insertionPoint++;
                                }
                            }
                        }
                    }
                    else if (token == "$__") {
                        setNextInsertionPoint = insertionPoint;
                        result.Insert(insertionPoint, "$_");
                        insertionPoint++;
                    }
                    else {
                        result.Insert(insertionPoint, token);
                        insertionPoint++;
                    }
                }

                if (setNextInsertionPoint >= 0)
                    insertionPoint = setNextInsertionPoint;

            }

            return result;
        }

        static readonly string[] identifierModifiers = new string[] {
            "$flat", "$snake", "$camel", "$big", "$tall", "$tower", "$midline",
            "$lonely", "$couple", "$triple", "$quadruple"
        };

        private static bool isCombinable(string s) {
            return s.Length > 0 && ((s[0] != '$' && isIdentifier(s[0])) || Array.IndexOf(identifierModifiers, s) != -1);
        }



        private static string mergeNames(List<string> words, int left, int right) {
            int first = defaultFirstCase, rest = defaultCase, glue = defaultGlue;
            int take = 0;

            string result = "";

            for (int i = left; i < right; i++) {
                if (words[i] == "$flat") {
                    first = low; rest = low; glue = concat;
                }
                else if (words[i] == "$snake") {
                    first = low; rest = low; glue = snake;
                }
                else if (words[i] == "$camel") {
                    first = low; rest = title; glue = concat;
                }
                else if (words[i] == "$big") {
                    first = title; rest = title; glue = concat;
                }
                else if (words[i] == "$tall") {
                    first = high; rest = high; glue = snake;
                }
                else if (words[i] == "$tower") {
                    first = high; rest = high; glue = concat;
                }
                else if (words[i] == "$midline") {
                    first = low; rest = low; glue = hyphen;
                }
                else if (words[i] == "$lonely") {
                    take = 1;
                }
                else if (words[i] == "$couple") {
                    take = 2;
                }
                else if (words[i] == "$triple") {
                    take = 3;
                }
                else if (words[i] == "$quadruple") {
                    take = 4;
                }
                else {
                    int casing = rest;
                    if (result.Length == 0) {
                        casing = first;
                    }
                    else {
                        if (glue == snake) {
                            result += "_";
                        }
                        else if (glue == hyphen) {
                            result += "-";
                        }
                    }

                    string toAdd = words[i];
                    if (take != 0) {
                        toAdd = toAdd.Substring(0, take);
                        take = 0;
                    }

                    if (toAdd.Length == 0) continue;

                    if (casing == low) {
                        toAdd = toAdd.ToLower();
                    }
                    else if (casing == high) {
                        toAdd = toAdd.ToUpper();
                    }
                    else if (casing == title) {
                        toAdd = (toAdd[0].ToString().ToUpper()) + (toAdd.Substring(1).ToLower());
                    }

                    result += toAdd;
                }
            }

            if (result.Length == 0) {
                result = "*";
            }
            return result;
        }

        private static List<string> combineIdentifiers(List<string> words, int insertionPoint, out int newInsertionPoint) {
            List<string> result = new List<string>();

            int left = 0;
            newInsertionPoint = 0;

            while (left < words.Count) {
                if (isCombinable(words[left])) {
                    int right = left + 1;
                    while (right < words.Count && isCombinable(words[right])) {
                        right++;
                    }

                    string name = mergeNames(words, left, right);
                    result.Add(name);

                    if (insertionPoint >= left && insertionPoint < right)
                        newInsertionPoint = result.Count;

                    left = right;
                }
                else {
                    if (insertionPoint == left)
                        newInsertionPoint = result.Count;

                    result.Add(words[left]);
                    left++;
                }
            }

            if (insertionPoint == words.Count)
                newInsertionPoint = result.Count;

            return result;
        }

        private static void extendFollowingSpace(ref string buildingResult) {
            if (buildingResult.Length > 0 && !isWhite(buildingResult[buildingResult.Length - 1])) {
                buildingResult += " ";
            }
        }

        private static string formatIntoCode(List<string> words, int insertionPoint) {
            string result = "";
            int indent = 0;
            bool omitNextSpace = false;

            // Special tokens:
            //    $line means, "hit enter".
            //    $up means, "increase the indent".
            //    $down means, "decrease the indent".

            for (int i = 0; i < words.Count; i++) {
                string token = words[i];

                if (i == insertionPoint) {
                    if (!omitNextSpace) extendFollowingSpace(ref result);
                    string ip = "▪";
                    if (token == "$_") {
                        ip = "■";
                    }
                    result += ip;
                }

                if (token == "$line") {
                    result += Environment.NewLine;
                    for (int k = 0; k < indent; k++)
                        result += "    ";
                }
                else if (token == "$up") {
                    indent++;
                }
                else if (token == "$down") {
                    indent = Math.Max(0, indent - 1);
                }
                else if (token == "$stop") {
                    // do nothing!
                }
                else if (token == "$glue") {
                    omitNextSpace = true;
                }
                else if (token == "$verbatim" && i < words.Count - 1 && words[i + 1].Length > 2 && words[i + 1][0] == '"') {
                    string content = words[i + 1].Substring(1, words[i + 1].Length - 2);
                    if (!omitNextSpace) extendFollowingSpace(ref result);
                    result += content;
                    i++;
                }
                else if (token == "$comment" && i < words.Count - 1 && words[i + 1].Length > 2 && words[i + 1][0] == '"') {
                    string content = words[i + 1].Substring(1, words[i + 1].Length - 2);
                    if (!omitNextSpace) extendFollowingSpace(ref result);
                    result += "// " + content + Environment.NewLine;
                    for (int k = 0; k < indent; k++)
                        result += "    ";
                    i++;
                }
                else {
                    if (!omitNextSpace) extendFollowingSpace(ref result);

                    if (token == "$_") {
                        if (i != insertionPoint) {
                            result += "□";
                        }
                    }
                    else {
                        string toAdd = (token[0] == '$' ? token.Substring(1) : token);
                        result += toAdd;
                    }
                }

                if (token != "$glue")
                    omitNextSpace = false;
            }


            if (words.Count == insertionPoint) {
                result += "▪";
            }

            return result;
        }

        public static string process(string code, bool final = false, bool inMath = false) {
            if (plainTextMode) {
                return code;
            }

            string result;

            if (mathMode && !inMath) {
                result = "";
                int i = 0;
                bool justNL = true;

                while (i < code.Length) {
                    if (code[i] == '–' || code[i] == '$') {
                        char delim = code[i];
                        bool mathBlock = (code[i] == '–');

                        i++;
                        int startMath = i;
                        while (i < code.Length && code[i] != delim) {
                            i++;
                        }

                        int k = i + 1;
                        bool justNLafter = true;

                        while (k < code.Length && code[k] != '\r' && code[k] != '\n') {
                            if (code[k] > 32) justNLafter = false;
                            k++;
                        }

                        justNL &= justNLafter;

                        if (mathBlock)
                            result += (justNL ? mathDelimitersLarge[0] : mathDelimitersSmall[0]);

                        if (i >= code.Length) {
                            result += process(code.Substring(startMath, i - startMath), false, true);
                        }
                        else {
                            result += process(code.Substring(startMath, i - startMath) + " decline all", true, true);
                        }

                        if (mathBlock)
                            result += (justNL ? mathDelimitersLarge[1] : mathDelimitersSmall[1]);

                        justNL = false;

                        if (i < code.Length && code[i] == delim)
                            i++;

                        if (i < code.Length && isAlpha(code[i]))
                            result += " ";
                    }
                    else {
                        if (code[i] == '\r' || code[i] == '\n') justNL = true;
                        else if (code[i] > 32) justNL = false;

                        if (code[i] == '\\') {
                            i++;
                            if (i < code.Length)
                                result += code[i];
                        }
                        else {
                            result += code[i];
                        }

                        i++;
                    }
                }
            }
            else {
                List<string> words = splitWords(code);
                words = makeSubstitutions(words, out int insertionPoint);
                words = combineIdentifiers(words, insertionPoint, out int newInsertionPoint);
                result = formatIntoCode(words, newInsertionPoint);

                if (debugWords) {
                    result += Environment.NewLine + Environment.NewLine;
                    for (int i = 0; i < words.Count; i++) {
                        if (insertionPoint == i) result += ">>";
                        result += words[i] + " ";
                    }
                }
            }

            if (final) {
                result = result.Replace("▪", "").Replace("□", "").Replace("■", "").Trim();
            }

            return result;
        }
    }
}
