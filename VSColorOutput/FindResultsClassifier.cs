// Copyright (c) 2012 Blue Onion Software. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

// Disable warning for unused ClassificationChanged event
#pragma warning disable 67

namespace BlueOnionSoftware
{
  public class FindResultsClassifier : IClassifier
  {
    private const string FindAll = "Find all \"";
    private const string MatchCase = "Match case";
    private const string WholeWord = "Whole word";
    private const string RegularExpressions = "Regular expressions";
    private const string ListFilenamesOnly = "List filenames only";
    private const string FindResults = "Find Results";

    private readonly IClassificationTypeRegistryService classificationRegistry;
    private static readonly Regex FilenameRegex;

    private Regex searchTextFirstLineRegex;
    private Regex searchTextRegex;

    static FindResultsClassifier()
    {
      FilenameRegex = new Regex(@"(^\s*.:.*\(\d+\):)|(^\s*.:.*)", RegexOptions.Compiled);
    }

    public FindResultsClassifier(IClassificationTypeRegistryService classificationRegistry)
    {
      this.classificationRegistry = classificationRegistry;
    }

    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
    {
      var classifications = new List<ClassificationSpan>();

      var snapshot = span.Snapshot;
      if (snapshot == null || snapshot.Length == 0 || !CanSearch(span))
      {
        return classifications;
      }

      var text = span.GetText();

      var filenameSpans   = GetMatches(text, FilenameRegex, span.Start, FilenameClassificationType).ToList();
      var searchTermSpans = GetMatches(text, text.StartsWith(FindAll) ? searchTextFirstLineRegex : searchTextRegex, span.Start, SearchTermClassificationType, text.StartsWith(FindAll)).ToList();

      var toRemove = (from searchSpan in searchTermSpans
                      from filenameSpan in filenameSpans
                      where filenameSpan.Span.Contains(searchSpan.Span)
                      select searchSpan).ToList();

      classifications.AddRange(filenameSpans);
      classifications.AddRange(searchTermSpans.Except(toRemove));
      return classifications;
    }

    private bool CanSearch(SnapshotSpan span)
    {
      if (span.Start.Position != 0 && searchTextRegex != null)
        return true;

      searchTextRegex = null;
      var firstLine = span.Snapshot.GetLineFromLineNumber(0).GetText();
      if (firstLine.StartsWith(FindAll))
      {
        int begin = FindAll.Length;
        int end = firstLine.LastIndexOf("\",", firstLine.LastIndexOf(FindResults));

        var strings = (from s in firstLine.Substring(end + 1).Split(',')
                       select s.Trim()).ToList();

        var start = strings[0].IndexOf('"');
        //var searchTerm = strings[0].Substring(start + 1, strings[0].Length - start - 2);
        var searchTerm = firstLine.Substring(begin, end - begin);

        var matchCase = strings.Contains(MatchCase);
        var matchWholeWord = strings.Contains(WholeWord);
        var filenamesOnly = strings.Contains(ListFilenamesOnly);
        var regularExpressions = strings.Contains(RegularExpressions);

        //if (!filenamesOnly)
        {
          //var regex = matchWholeWord ? string.Format(@"\b{0}\b", Regex.Escape(searchTerm)) : Regex.Escape(searchTerm);
          var regex = regularExpressions ? searchTerm : Regex.Escape(searchTerm);
          var casing = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
          searchTextRegex          = new Regex(regex, RegexOptions.None | casing);
          searchTextFirstLineRegex = new Regex(Regex.Escape(searchTerm), RegexOptions.None | casing);

          return true;
        }
      }
      return false;
    }

    private static IEnumerable<ClassificationSpan> GetMatches(
      string text, Regex regex, SnapshotPoint snapStart, IClassificationType classificationType, bool onlyFirst = false)
    {
      var list = new List<ClassificationSpan>();

      foreach (Match match in regex.Matches(text).Cast<Match>())
      {
        list.Add(new ClassificationSpan(new SnapshotSpan(snapStart + match.Index, match.Length), classificationType));
        if (onlyFirst)
          break;
      }

      return list;
    }

    private IClassificationType SearchTermClassificationType
    {
      get { return classificationRegistry.GetClassificationType(OutputClassificationDefinitions.FindResultsSearchTerm); }
    }

    private IClassificationType FilenameClassificationType
    {
      get { return classificationRegistry.GetClassificationType(OutputClassificationDefinitions.FindResultsFilename); }
    }

    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
  }
}
