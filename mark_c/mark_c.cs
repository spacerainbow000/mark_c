using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

namespace mark_c
{
	public class MarkovChainController
	{
		private Random r;
		private MarkovDictionary dictionary;

		public MarkovChainController()
		{
			this.dictionary = new MarkovDictionary();
		}
		public MarkovChainController(MarkovDictionary dictionary)
		{
			this.dictionary = dictionary;
		}

		public void add(String word)
		{
			dictionary.addWord(word);
		}

		public void remove(String word)
		{
			dictionary.removeWord(word);
		}

		public void assoc(String word, String assoc)
		{
			dictionary.associate(word, assoc);
		}

		public void assocendl(String word)
		{
			dictionary.associateTail(word);
		}

		public String read(String word)
		{
			DictionaryEntry entry = dictionary.getEntry(word);
			DictionaryAssociation assoc = new DictionaryAssociation();

			uint n = (uint)r.Next(0, (int)entry.totalAssocs);

			foreach (DictionaryAssociation a in entry.assocs) //weighted select
			{
				if (n < entry.totalAssocs)
				{
					assoc = a;
					break;
				}

				n -= entry.totalAssocs;
			}

			if (assoc.isTail())
			{
				MarkovException e = new MarkovException();
				e.word = word;
				e.toggleTail();
				throw e;
			}

			return assoc.getWord();
		}

		public String readFromStart()
		{
			DictionaryEntry entry = dictionary.getHead();
			DictionaryAssociation assoc = new DictionaryAssociation();

			uint n = (uint)r.Next(0, (int)entry.totalAssocs);

			foreach (DictionaryAssociation a in entry.assocs)
			{
				if (n < entry.totalAssocs)
				{
					assoc = a;
					break;
				}

				n -= entry.totalAssocs;
			}

			if (assoc.isTail())
			{
				MarkovException e = new MarkovException();
				e.toggleTail();
				throw e;
			}

			return assoc.getWord();
		}

		public String walk(int count)
		{
			String result = "";
			List<String> resultBuilder = new List<string>();

			resultBuilder.Add(readFromStart());

			try
			{
				for (int i = 0; i < count; i++)
					resultBuilder.Add(read(resultBuilder.Last()));
			}
			catch (MarkovException e)
			{
				if (!e.isHead)
					throw e;
			}
			catch (Exception e)
			{
				throw e;
			}

			foreach (String m in resultBuilder)
				result += m + " ";

			return result;
		}

		public String walkFrom(String word, int count)
		{
			String result = word;
			List<String> resultBuilder = new List<string>();
			resultBuilder.Add(result);

			try
			{
				for (int i = 0; i < count; i++)
					resultBuilder.Add(read(resultBuilder.Last()));
			}
			catch (MarkovException e)
			{
				if (!e.isHead)
					throw e;
			}
			catch (Exception e)
			{
				throw e;
			}

			foreach (String m in resultBuilder)
				result += m + " ";

			return result;
		}
	}

	[Serializable()]
	public class MarkovDictionary
	{
		private List<DictionaryEntry> entries;

		public MarkovDictionary()
		{
			entries = new List<DictionaryEntry>();
			entries.Add(new DictionaryEntry()); //add head
		}
		public MarkovDictionary(MarkovDictionary d)
		{
			entries = d.entries;
		}

		internal List<DictionaryEntry> getAllEntries()
		{
			List<DictionaryEntry> e = new List<DictionaryEntry>(entries);
			return e;
		}
		internal DictionaryEntry getEntry(String word)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries.ElementAt(i).getWord() == word)
					return entries.ElementAt(i);
			}

			MarkovException e = new MarkovException(word);
			throw e;
		}
		internal DictionaryEntry getHead()
		{
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries.ElementAt(i).isHead())
					return entries.ElementAt(i);
			}

			MarkovException e = new MarkovException();
			e.toggleHead();
			throw e;
		}

		public void addWord(String word)
		{
			entries.Add(new DictionaryEntry(word));
		}
		public void removeWord(String word)
		{
			Boolean found = false;

			for (int i = 0; i < entries.Count; i++)
			{
				if (entries.ElementAt(i).isHead())
					continue; //don't want to get annoying null ref related issues

				if (entries.ElementAt(i).getWord().Equals(word))
				{
					entries.RemoveAt(i);
					found = true;
					break;
				}
			}

			if (found)
				deassociate(word);
		}
		public void removeTailAssociationsFor(String word)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries.ElementAt(i).isHead())
					continue;

				if (entries.ElementAt(i).getWord().Equals(word))
				{
					entries[i].removeTailAssociation();
					break;
				}
			}
		}
		public void removeTailAssociationsForHead()
		{
			for (int i = 0; i < entries.Count; i++) //should be at 0 but jic it moves
				if (entries.ElementAt(i).isHead())
				{
					entries[i].removeTailAssociation();
					break;
				}
		}
		private void deassociate(String word)
		{
			//remove ALL references to the word -- can't have refs to words that aren't in dic anymore
			for (int i = 0; i < entries.Count; i++)
				entries[i].removeAssociation(word);
		}

		public void associate(String word, String wordFollowing)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries.ElementAt(i).isHead())
					continue;

				if (entries.ElementAt(i).getWord().Equals(word))
				{
					entries[i].associate(wordFollowing);
					return;
				}
			}

			addWord(word);
			associate(word, wordFollowing);
		}
		public void associateTail(String word)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries.ElementAt(i).isHead())
					continue;

				if (entries.ElementAt(i).getWord().Equals(word))
				{
					entries[i].associateTail();
					return;
				}
			}

			addWord(word);
			associateTail(word);
		}
		public void associateHeadWith(String wordFollowing)
		{
			for (int i = 0; i < entries.Count; i++) //should be at 0 but jic it moves
			{
				if (entries.ElementAt(i).isHead())
				{
					entries[i].associate(wordFollowing);
					return;
				}
			}
		}
		public void associateHeadWithTail()
		{
			for (int i = 0; i < entries.Count; i++) //should be at 0 but jic it moves
			{
				if (entries.ElementAt(i).isHead())
				{
					entries[i].associateTail();
					return;
				}
			}
		}
	}

	internal class DictionaryEntry
	{
		internal enum special { HEAD, TAIL, NOSTATUS };
		private special status; //only ever normal or head

		private String key;

		internal List<DictionaryAssociation> assocs;
		internal uint totalAssocs = 0;

		//init
		public DictionaryEntry(String word)
		{
			key = word;
			assocs = new List<DictionaryAssociation>();
			status = special.NOSTATUS;
		}
		public DictionaryEntry() //empty constructor creates head node
		{
			key = null;
			assocs = new List<DictionaryAssociation>();
			status = special.HEAD;
		}

		public String getWord()
		{
			return key; //make sure entry is not head first!
		}

		//always call!
		public Boolean isHead()
		{
			return (status == special.HEAD) ? true : false;
		}

		//number of times 'word' follows the key
		public UInt32 getCount(String word)
		{
			for (int i = 0; i < assocs.Count; i++)
			{
				if (assocs.ElementAt(i).isTail())
					continue;

				if (assocs.ElementAt(i).getWord().Equals(word))
					return assocs.ElementAt(i).getCount();
			}

			//no matches
			return 0;
		}
		public UInt32 getTailCount()
		{
			for (int i = 0; i < assocs.Count; i++)
			{
				if (assocs.ElementAt(i).isTail())
					return assocs.ElementAt(i).getCount();
			}

			//no matches, never followed by end of seq
			return 0;
		}

		//manip assoc counts
		public void associate(String word)
		{
			for (int i = 0; i < assocs.Count; i++)
			{
				if (assocs.ElementAt(i).isTail())
					continue;

				if (assocs.ElementAt(i).getWord().Equals(word))
				{
					assocs[i].inc();
					break;
				}
			}
			assocs.Add(new DictionaryAssociation(word)); //new reference

			totalAssocs++;
		}
		public void associateTail()
		{
			Boolean found = false;

			for (int i = 0; i < assocs.Count; i++)
			{
				if (assocs.ElementAt(i).isTail())
				{
					assocs[i].inc();
					found = true;
				}
			}

			if (!found)
				assocs.Add(new DictionaryAssociation());
		}
		public void removeAssociation(String word)
		{
			for (int i = 0; i < assocs.Count; i++)
			{
				if (assocs.ElementAt(i).isTail())
					continue;

				if (assocs.ElementAt(i).getWord().Equals(word))
				{
					totalAssocs -= assocs.ElementAt(i).getCount();
					assocs.RemoveAt(i);
					break;
				}
			}
		}
		public void removeTailAssociation()
		{
			for (int i = 0; i < assocs.Count; i++)
				if (assocs.ElementAt(i).isTail())
				{
					totalAssocs -= assocs.ElementAt(i).getCount();
					assocs.RemoveAt(i);
					break;
				}
		}
	}

	internal class DictionaryAssociation
	{
		private String word;
		private UInt32 count;
		private DictionaryEntry.special status; //only ever normal or tail

		//init with no args to create end-of-seq assoc
		public DictionaryAssociation(String word)
		{
			this.word = word;
			count = 1;
			status = DictionaryEntry.special.NOSTATUS;
		}
		public DictionaryAssociation()
		{
			this.status = DictionaryEntry.special.TAIL;
			count = 1;
		}

		public String getWord()
		{
			return word;
		}
		public UInt32 getCount()
		{
			return count;
		}

		//increment hit count
		public void inc()
		{
			count++;
		}

		//always call!
		public Boolean isTail()
		{
			if (status == DictionaryEntry.special.TAIL)
				return true;
			return false;
		}
	}
}

public class MarkovException : Exception
{
	public String word;
	public bool isHead;
	public bool isTail;

	public MarkovException() : base("there was a dictionary-related exception -- UNKNOWN")
	{
		word = "";
		isHead = false;
		isTail = false;
	}
	public MarkovException(String word) : base("there was a dictionary-related exception -- WORDNOTFOUND")
	{
		this.word = word;
		isHead = false;
		isTail = false;
	}

	public void toggleHead()
	{
		isHead = !isHead;
	}
	public void toggleTail()
	{
		isTail = !isTail;
	}
}