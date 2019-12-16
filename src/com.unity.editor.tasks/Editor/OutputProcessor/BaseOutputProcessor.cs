// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;

namespace Unity.Editor.Tasks
{
	using Logging;

	public interface IOutputProcessor
	{
		void Process(string line);
	}

	public interface IOutputProcessor<T> : IOutputProcessor
	{
		event Action<T> OnEntry;
		T Result { get; }
	}

	public interface IOutputProcessor<TData, T> : IOutputProcessor<T>
	{
		new event Action<TData> OnEntry;
	}

	public class BaseOutputProcessor<T> : IOutputProcessor<T>
	{
		public delegate T3 FuncO<in T1, T2, out T3>(T1 arg1, out T2 out1);

		private Func<string, T> converter;
		private readonly FuncO<string, T, bool> handler;

		private static readonly bool IsString = typeof(T) == typeof(string);

		private ILogging logger;
		public event Action<T> OnEntry;

		public BaseOutputProcessor() {}

		public BaseOutputProcessor(FuncO<string, T, bool> handler)
		{
			this.handler = handler;
		}

		public BaseOutputProcessor(Func<string, T> converter)
		{
			this.converter = converter;
		}

		public void Process(string line)
		{
			LineReceived(line);
		}

		protected virtual void LineReceived(string line)
		{
			if (handler != null)
			{
				if (handler(line, out var result))
					RaiseOnEntry(result);
				return;
			}

			if (converter != null)
			{
				// if there's a converter, all results it returns are valid
				var result = ConvertResult(line);
				RaiseOnEntry(result);
				return;
			}

			if (ProcessLine(line, out var entry))
				RaiseOnEntry(entry);
		}

		protected virtual T ConvertResult(string line)
		{
			if (converter != null)
				return converter(line);
			else if (IsString)
				return (T)(object)line;
			return default;
		}

		protected virtual bool ProcessLine(string line, out T result)
		{
			result = ConvertResult(line);
			// if T is string, no conversion is needed, result is valid
			if (IsString) return true;
			return false;
		}

		protected void RaiseOnEntry(T entry)
		{
			Result = entry;
			OnEntry?.Invoke(entry);
		}

		public virtual T Result { get; protected set; }
		protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
	}

	public class BaseOutputProcessor<TData, T> : BaseOutputProcessor<T>, IOutputProcessor<TData, T>
	{
		public new event Action<TData> OnEntry;
		private Func<string, TData> converter;
		private readonly FuncO<string, TData, bool> handler;
		private static readonly bool IsString = typeof(T) == typeof(string);

		public BaseOutputProcessor() {}

		public BaseOutputProcessor(FuncO<string, TData, bool> handler)
		{
			this.handler = handler;
		}

		public BaseOutputProcessor(Func<string, TData> converter)
		{
			this.converter = converter;
		}

		protected override void LineReceived(string line)
		{
			if (handler != null)
			{
				if (handler(line, out TData result))
					RaiseOnEntry(result);
				return;
			}

			if (converter != null)
			{
				// if there's a converter, all results it returns are valid
				var result = ConvertResult(line);
				RaiseOnEntry(result);
				return;
			}

			if (ProcessLine(line, out TData entry))
				RaiseOnEntry(entry);
		}

		protected new virtual TData ConvertResult(string line)
		{
			if (converter != null)
				return converter(line);
			else if (IsString)
				return (TData)(object)line;
			return default;
		}

		protected virtual bool ProcessLine(string line, out TData result)
		{
			result = ConvertResult(line);
			// if T is string, no conversion is needed, result is valid
			if (IsString) return true;
			return false;
		}

		protected virtual void RaiseOnEntry(TData entry)
		{
			OnEntry?.Invoke(entry);
		}
	}

	public abstract class BaseOutputListProcessor<T> : BaseOutputProcessor<T, List<T>>
	{
		public BaseOutputListProcessor() { }

		public BaseOutputListProcessor(FuncO<string, T, bool> handler) : base(handler)
		{}

		public BaseOutputListProcessor(Func<string, T> converter)
		{}

		protected override void RaiseOnEntry(T entry)
		{
			if (Result == null)
			{
				Result = new List<T>();
			}
			Result.Add(entry);
			base.RaiseOnEntry(entry);
		}
	}
}
