using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieNode
    {
        public Dictionary<char, TrieNode> children;
        public Boolean isWord;
        public TrieNode()
        {
            children = new Dictionary<char, TrieNode>();
            isWord = false;
        }

        public TrieNode GetChild(char c)
        {
            if (children.ContainsKey(c))
            {
                return children[c];
            }
            return null;
        }



    }
}