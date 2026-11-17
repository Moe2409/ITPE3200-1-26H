using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Security.Claims;
using System.Linq.Expressions;
using Gremlin.Controllers;
using Gremlin.ViewModels;
using Gremlin.Models;

namespace Gremlin.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
        private readonly Mock<SignInManager<IdentityUser>> _mockSignInManager;
        private readonly Mock<GremlinDbContext> _mockContext;
        private readonly Mock<ILogger<AccountController>> _mockLogger;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            
            var userStoreMock = new Mock<IUserStore<IdentityUser>>();
            _mockUserManager = new Mock<UserManager<IdentityUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            _mockSignInManager = new Mock<SignInManager<IdentityUser>>(
                _mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

            
            var dbContextOptions = new DbContextOptions<GremlinDbContext>();
            _mockContext = new Mock<GremlinDbContext>(dbContextOptions);

            var mockQuizzes = CreateMockDbSet(new List<Quiz>().AsQueryable());
            _mockContext.Setup(c => c.Quizzes).Returns(mockQuizzes.Object);

            var mockHistories = CreateMockDbSet(new List<History>().AsQueryable());
            _mockContext.Setup(c => c.Histories).Returns(mockHistories.Object);

            _mockLogger = new Mock<ILogger<AccountController>>();

            
            _controller = new AccountController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockContext.Object,
                _mockLogger.Object
            );

            
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(true);
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/Home/Index");

            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
            _controller.Url = mockUrlHelper.Object;
            _controller.ControllerContext = new ControllerContext(actionContext);
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
            return mockSet;
        }

        
        // 6 POSITIVE TESTS 
        

        [Fact]
        public async Task Register_ValidModel_CreatesUserAndRedirects()
        {
            var model = new RegisterViewModel { UserName = "testuser", Email = "test@test.com", Password = "Password123!" };
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockSignInManager.Setup(x => x.SignInAsync(It.IsAny<IdentityUser>(), false, null))
                .Returns(Task.CompletedTask);

            var result = await _controller.Register(model);

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task Login_ValidCredentials_SignsInAndRedirects()
        {
            var model = new LoginViewModel { Email = "test@test.com", Password = "Password123!" };
            var user = new IdentityUser { UserName = "testuser", Email = model.Email };

            _mockUserManager.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _mockSignInManager.Setup(x => x.PasswordSignInAsync(user.UserName, model.Password, false, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var result = await _controller.Login(model);

            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Contains("/Home", redirectResult.Url);
        }

        [Fact]
        public async Task Logout_ValidSession_SignsOutAndRedirects()
        {
            _mockSignInManager.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

            var result = await _controller.Logout();

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task Profile_ExistingUser_ReturnsViewWithUserDetails()
        {
            string username = "existinguser";
            var user = new IdentityUser { Id = "1", UserName = username };

            _mockUserManager.Setup(x => x.FindByNameAsync(username)).ReturnsAsync(user);

            var result = await _controller.Profile(username);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("UserDetails", viewResult.ViewName);
        }

        [Fact]
        public async Task Update_Get_ExistingUser_ReturnsViewWithModel()
        {
            var user = new IdentityUser { Id = "1", UserName = "currentuser" };
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _controller.Update();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<UpdateAccountViewModel>(viewResult.Model);
            Assert.Equal("currentuser", model.UserName);
        }

        [Fact]
        public async Task DeleteConfirmed_ExistingUser_CleansUpAndRedirects()
        {
            var user = new IdentityUser { Id = "1", UserName = "deleteuser" };
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockSignInManager.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

            var result = await _controller.DeleteConfirmed();

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }


        // 6 NEGATIVE TESTS 
        

        [Fact]
        public async Task Register_InvalidModelState_ReturnsViewWithoutCreatingUser()
        {
            _controller.ModelState.AddModelError("Email", "Required");
            var model = new RegisterViewModel();

            var result = await _controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_UserNotFound_ReturnsViewWithInvalidLoginError()
        {
            var model = new LoginViewModel { Email = "notfound@test.com", Password = "Password123!" };
            _mockUserManager.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync((IdentityUser?)null);

            var result = await _controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
        }

        [Fact]
        public async Task Login_AccountLockedOut_ReturnsViewWithLockoutError()
        {
            var model = new LoginViewModel { Email = "locked@test.com", Password = "Password123!" };
            var user = new IdentityUser { UserName = "lockeduser", Email = model.Email };

            _mockUserManager.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _mockSignInManager.Setup(x => x.PasswordSignInAsync(user.UserName, model.Password, false, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            var result = await _controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
        }

        [Fact]
        public async Task Profile_NonExistentUser_ReturnsNotFound()
        {
            string username = "ghost";
            _mockUserManager.Setup(x => x.FindByNameAsync(username)).ReturnsAsync((IdentityUser?)null);

            var result = await _controller.Profile(username);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_Get_UserNotFound_ReturnsNotFound()
        {
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser?)null);

            var result = await _controller.Update();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteConfirmed_UserNotFound_ReturnsNotFound()
        {
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser?)null);

            var result = await _controller.DeleteConfirmed();

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFoundResult.Value);
        }
    }

       internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
        public T Current => _inner.Current;
    }

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;
        public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
        public object Execute(Expression expression) => _inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })?
                .MakeGenericMethod(expectedResultType)
                .Invoke(this._inner, new[] { expression });
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))?
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult })!;
        }
    }
}