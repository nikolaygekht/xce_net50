using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public static class EnumerableAssertionExtension
    {
        public static AndConstraint<GenericCollectionAssertions<T>> ContainOnly<T>(this GenericCollectionAssertions<T> assertion, Expression<Func<T, bool>> predicate, string because = null, params object[] becauseArgs)
        {
            var func = predicate.Compile();

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => assertion.Subject)
                .ForCondition(collection => collection.All(func))
                .FailWith("Expected collection have all elements matching the predicated {0} but it has the element {1} that does not match", predicate, assertion.Subject.FirstOrDefault(p => !func(p)));

            return new AndConstraint<GenericCollectionAssertions<T>>(assertion);
        }
    }
}
