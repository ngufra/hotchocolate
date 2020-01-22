using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    public class SortQueryableClosure
    {
        private readonly bool _inMemory;

        public SortQueryableClosure(
            Type type,
            string parameterName,
            bool inMemory)
        {
            Parameter = Expression.Parameter(type, parameterName);
            Instance = new Stack<Expression>();
            Property = new Stack<PropertyInfo>();

            Instance.Push(Parameter);
            _inMemory = inMemory;
        }

        public ParameterExpression Parameter { get; }

        private Stack<Expression> Instance { get; }
        private Stack<PropertyInfo> Property { get; }

        public SortOperationInvocation CreateSortOperation(SortOperationKind kind)
        {
            if (_inMemory)
            {
                return CreateInMemorySortOperation(kind);
            }
            return new SortOperationInvocation(kind, Parameter, Instance.Peek());
        }
        public void EnqueueProperty(PropertyInfo property)
        {
            Property.Push(property);
            Instance.Push(Expression.Property(Instance.Peek(), property));
        }

        public Expression Pop()
        {
            Property.Pop();
            return Instance.Pop();
        }

        private SortOperationInvocation CreateInMemorySortOperation(
            SortOperationKind kind)
        {
            Expression nextExpression = Instance.Peek();
            if (Property.Count > 0)
            {
                DefaultExpression defaultOfType =
                    Expression.Default(Property.Peek().PropertyType);
                Stack<Expression>.Enumerator enumerator = Instance.GetEnumerator();
                enumerator.MoveNext();
                while (enumerator.MoveNext())
                {
                    nextExpression =
                        SortExpressionBuilder.IfNullThenDefault(
                            enumerator.Current, nextExpression, defaultOfType);
                }
            }
            return new SortOperationInvocation(kind, Parameter, nextExpression);
        }

    }
}
