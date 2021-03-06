﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Scrumfish.OData.Client.Common
{
    public class ODataQuery<T>
    {
        private readonly StringBuilder _uri;
        private string CurrentOperation { get; set; }
        private int SubQueryLevel { get; set; }
        private bool InQuery { get; set; }
        private bool IsStart { get; set; }
        private bool IsSubQuery { get; }
        private bool IsEmpty { get; set; } = true;
        private HashSet<string> Operations { get; } = new HashSet<string>();
        private HashSet<string> Aliases { get; } = new HashSet<string>();


        internal ODataQuery(bool isSubquery)
        {
            _uri = new StringBuilder(512);
            IsStart = true;
            InQuery = true;
            IsSubQuery = isSubquery;
        } 

        internal ODataQuery(string uri)
        {
            _uri = new StringBuilder(uri, 512);
            IsStart = uri.EndsWith("?");
            InQuery = IsStart;
        }

        internal ODataQuery(Uri uri)
        {
            var uriString = uri.ToString();
            _uri = new StringBuilder(uriString, 512);
            IsStart = uriString.EndsWith("?");
            InQuery = IsStart;
        }

        public override string ToString()
        {
            if (SubQueryLevel != 0)
            {
                throw new InvalidExpressionException("Subquery start and end mismatch.");
            }
            return _uri.ToString();
        }

        public Uri ToUri()
        {
            return new Uri(this.ToString());
        }

        internal ODataQuery<T> AppendOperation(string operation)
        {
            if (Operations.Contains(operation))
            {
                throw new InvalidOperationException($"{operation} has alread been added to query.");
            }
            if (!operation.StartsWith("$"))
            {
                throw new InvalidOperationException($"{operation} is not a valid OData operation.");
            }
            CurrentOperation = operation;
            Operations.Add(operation);
            AppendUriElement(operation);
            return this;
        }

        private void AppendUriElement(string operation)
        {
            if (!IsStart)
            {
                _uri.Append(IsSubQuery ? ';' : '&');
            }
            IsStart = false;
            _uri.Append(operation);
        }

        internal ODataQuery<T> AssertCurrentOperation(string operation)
        {
            if (CurrentOperation != operation)
            {
                throw new InvalidExpressionException($"Expected {operation} but was in {CurrentOperation}.");
            }
            return this;
        }

        internal ODataQuery<T> AppendExpression<TV>(TV expression)
        {
            _uri.Append('=')
                .Append(expression);
            return this;
        }

        internal ODataQuery<T> AppendChainingExpression<TV>(TV expression)
        {
            _uri.Append(',')
                .Append(expression);
            return this;
        }

        internal ODataQuery<T> AppendModifier<TV>(TV modifier)
        {
            _uri.Append(modifier);
            return this;
        }

        internal ODataQuery<T> StartSubQuery()
        {
            if (IsStart)
            {
                throw new InvalidExpressionException("Cannot start a sub-query at the start of another query.");
            }
            IsStart = true;
            IsEmpty = true;
            SubQueryLevel++;
            _uri.Append('(');
            return this;
        }

        internal ODataQuery<T> EndSubQuery()
        {
            if (IsEmpty)
            {
                throw new InvalidExpressionException("Cannot create an empty sub-query.");
            }
            if (--SubQueryLevel < 0)
            {
                throw new InvalidExpressionException("Cannot end a sub-query when no sub-query started.");
            }
            _uri.Append(')');
            return this;
        }

        internal ODataQuery<T> AppendQuery<TY>(ODataQuery<TY> subquery)
        {
            AssertCurrentOperation("$expand");
            IsEmpty = false;
            _uri.Append(subquery);
            return this;
        }

        internal ODataQuery<T> AppendAlias(string aliasName)
        {
            if (Aliases.Contains(aliasName))
            {
                throw new InvalidOperationException($"{aliasName} has alread been added to query.");
            }
            if (!aliasName.StartsWith("@"))
            {
                throw new InvalidOperationException($"{aliasName} is not a valid OData alias.");
            }
            Aliases.Add(aliasName);
            AppendUriElement(aliasName);
            return this;
        }
    }
}
