using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieTree
    {
        private TrieNode root;

        public TrieTree()
        {
            this.root = new TrieNode();
        }

        public TrieNode getRoot()
        {
            return root;
        }

        public void addTitle(string word)
        {
            TrieNode node = root;
            addTitle(word, node);
        }

        private void addTitle(string word, TrieNode node)
        {
            if (word.Length == 1)
            {
                char c = char.ToLower(word[0]);
                if (!node.children.ContainsKey(c))
                {
                    node.children.Add(c, new TrieNode());
                }
                node = node.GetChild(c);
                node.isWord = true;
            }
            if (word.Length > 1)
            {
                char c = char.ToLower(word[0]);
                word = word.Substring(1);
                if (node.children.ContainsKey(c))
                {
                    addTitle(word, node.GetChild(c));
                }
                else
                {
                    node.children.Add(c, new TrieNode());
                    node = node.GetChild(c);
                    addTitle(word, node);
                }
            }
        }

        public List<string> searchTitle(string word)
        {
            List<string> result = new List<string>();
            if (word.Length < 1)
            {
                return result;
            }
            TrieNode node = root;
            word = word.ToLower();
            for (int i = 0; i < word.Length; i++)
            {
                char lower = word[i];
                if (node.GetChild(lower) != null)
                {
                    node = node.children[lower];
                }
                else
                {
                    return result;
                }               
            }
            return searchTitle(word, node, result);
        }

        private List<string> searchTitle(string temp, TrieNode node, List<string> result)
        {
            try
            {

                if (result.Count >= 10)
                {
                    return result;
                }

                if (node.isWord)
                {
                    result.Add(temp);
                }

                foreach (char c in node.children.Keys)
                {
                    searchTitle(temp + c, node.children[c], result);
                }
            }
            catch(KeyNotFoundException){ }
            return result;
        }

    }
}