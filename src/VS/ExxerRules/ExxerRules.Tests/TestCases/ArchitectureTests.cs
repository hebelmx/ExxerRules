using System.Collections.Immutable;
using ExxerRules.Analyzers;
using ExxerRules.Analyzers.Architecture;
using ExxerRules.Tests.Testing;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace ExxerRules.Tests.TestCases;

/// <summary>
/// Test cases for architecture analyzers.
/// SRP: Contains only test cases related to architecture validation.
/// </summary>
public class ArchitectureTests
{
	/// <summary>
	/// Tests that using repository pattern does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_UsingRepositoryPattern()
	{
		const string testCode = @"
using System;
using System.Collections.Generic;

namespace TestProject
{
	public interface IUserRepository
	{
		User GetById(int id);
		IEnumerable<User> GetAll();
		void Add(User user);
	}

	public class UserRepository : IUserRepository
	{
		public User GetById(int id) { return new User(); }
		public IEnumerable<User> GetAll() { return new List<User>(); }
		public void Add(User user) { }
	}

	public class UserService
	{
		private readonly IUserRepository _repository;

		public UserService(IUserRepository repository)
		{
			_repository = repository;
		}

		public User GetUser(int id)
		{
			return _repository.GetById(id);
		}
	}

	public class User { }
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseRepositoryPatternAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests that direct DbContext usage reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_UsingDbContextDirectly()
	{
		const string testCode = @"
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace TestProject
{
	public class UserService
	{
		private readonly ApplicationDbContext _context;

		public UserService(ApplicationDbContext context)
		{
			_context = context;
		}

		public User GetUser(int id)
		{
			return _context.Users.FirstOrDefault(u => u.Id == id);
		}
	}

	public class ApplicationDbContext : DbContext
	{
		public DbSet<User> Users { get; set; }
	}

	public class User { public int Id { get; set; } }
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseRepositoryPatternAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseRepositoryPattern).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that domain referencing infrastructure reports diagnostic.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DomainReferencesInfrastructure()
	{
		const string testCode = @"
using Microsoft.EntityFrameworkCore;

namespace TestProject.Domain
{
	public class User
	{
		public int Id { get; set; }
		public string Name { get; set; }
		
		public void SaveToDatabase(ApplicationDbContext context)
		{
			context.Users.Add(this);
			context.SaveChanges();
		}
	}

	public class ApplicationDbContext : DbContext
	{
		public DbSet<User> Users { get; set; }
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DomainShouldNotReferenceInfrastructureAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.DomainShouldNotReferenceInfrastructure).ShouldBeTrue();
	}

	/// <summary>
	/// Tests that domain not referencing infrastructure does not report diagnostic.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_DomainDoesNotReferenceInfrastructure()
	{
		const string testCode = @"
namespace TestProject.Domain
{
	public class User
	{
		public int Id { get; set; }
		public string Name { get; set; }
		
		public void Validate()
		{
			if (string.IsNullOrEmpty(Name))
				throw new System.ArgumentException(""Name cannot be empty"");
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DomainShouldNotReferenceInfrastructureAnalyzer());
		diagnostics.Length.ShouldBe(0);
	}

	/// <summary>
	/// Tests edge case: Domain namespace with different patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_DomainNamespaceWithDifferentPatterns()
	{
		const string testCode = @"
using Microsoft.EntityFrameworkCore;

namespace MyApp.Domain.Services
{
	public class UserService
	{
		public void ProcessUser()
		{
			// This should be detected
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DomainShouldNotReferenceInfrastructureAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.DomainShouldNotReferenceInfrastructure).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Infrastructure namespace with different patterns.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_InfrastructureNamespaceWithDifferentPatterns()
	{
		const string testCode = @"
using System.Data.SqlClient;

namespace MyApp.Domain.Core
{
	public class UserService
	{
		public void ProcessUser()
		{
			// This should be detected
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new DomainShouldNotReferenceInfrastructureAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.DomainShouldNotReferenceInfrastructure).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Repository class without interface.
	/// </summary>
	[Fact]
	public void Should_ReportDiagnostic_When_RepositoryClassWithoutInterface()
	{
		const string testCode = @"
using Microsoft.EntityFrameworkCore;

namespace TestProject
{
	public class UserRepository
	{
		private readonly DbContext _context;

		public UserRepository(DbContext context)
		{
			_context = context;
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseRepositoryPatternAnalyzer());
		diagnostics.Length.ShouldBeGreaterThanOrEqualTo(1);
		diagnostics.Any(d => d.Id == DiagnosticIds.UseRepositoryPattern).ShouldBeTrue();
	}

	/// <summary>
	/// Tests edge case: Repository class with interface.
	/// </summary>
	[Fact]
	public void Should_NotReportDiagnostic_When_RepositoryClassWithInterface()
	{
		const string testCode = @"
using Microsoft.EntityFrameworkCore;

namespace TestProject
{
	public interface IUserRepository
	{
		void GetUser();
	}

	public class UserRepository : IUserRepository
	{
		private readonly DbContext _context;

		public UserRepository(DbContext context)
		{
			_context = context;
		}

		public void GetUser()
		{
			// Implementation
		}
	}
}";

		var diagnostics = AnalyzerTestHelper.RunAnalyzer(testCode, new UseRepositoryPatternAnalyzer());
		// Should not report diagnostic for repository with interface
		diagnostics.Length.ShouldBe(0);
	}
}
