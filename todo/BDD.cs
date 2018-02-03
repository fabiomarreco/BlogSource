using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Test.BDD
{
    //State
    //given
    // when
    //then
    public static class BDD
    {
        public static TState Create<TState, TGiven, TWhen, TThen>(
            Action<TState> state,
            Action<TState,  TGiven> given,
            Action<TState,  TWhen> when,
            Action<TState,  TThen> then)
        {

        }
    }


    public interface ScenarioResult<TGiven, TWhen, TThen>
    {
        IGivenResult<TGiven, TWhen, TThen> Given(Expression<Func<TGiven>> g);
    }

    public interface IGivenResult<TGiven, TWhen, TThen>
    {
        IGivenResult<TGiven, TWhen, TThen> And(Expression<Func<TGiven>> g);
        IWhenResult<TWhen, TThen> When(Expression<Func<TWhen>> w);
    }

    public interface IWhenResult<TWhen, TThen>
    {
        
    }
}
