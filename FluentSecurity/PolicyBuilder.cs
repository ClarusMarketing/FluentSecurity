using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq.Expressions;
using System.Web.Mvc;

namespace FluentSecurity
{
	public class PolicyBuilder : Builder<IPolicyContainer>
	{
		private Func<bool> _isAuthenticatedFunction;
		private Func<object[]> _rolesFunction;

		public IPolicyContainer For<TController>(Expression<Func<TController, object>> propertyExpression) where TController : Controller
		{
			ValidateConfiguration();

			var controllerName = typeof(TController).GetControllerName();
			var actionName = propertyExpression.GetActionName();

			return AddPolicyContainerFor(controllerName, actionName);
		}

		public IConventionPolicyContainer For<TController>() where TController : Controller
		{
			ValidateConfiguration();

			var controllerType = typeof(TController);
			var controllerName = controllerType.GetControllerName();
			var actionMethods = controllerType.GetActionMethods();

			var policyContainers = new List<IPolicyContainer>();
			foreach (var actionMethod in actionMethods)
			{
				var actionName = actionMethod.Name;
				var policyContainer = AddPolicyContainerFor(controllerName, actionName);
				policyContainers.Add(policyContainer);
			}

			return new ConventionPolicyContainer(policyContainers);
		}

		private void ValidateConfiguration()
		{
			if (_isAuthenticatedFunction == null)
				throw new ConfigurationErrorsException("You must specify a function returning authenticationstatus before adding policies.");
		}

		private IPolicyContainer AddPolicyContainerFor(string controllerName, string actionName)
		{
			IPolicyContainer policyContainer;

			var existingContainer = _itemValues.GetContainerFor(controllerName, actionName);
			if (existingContainer != null)
			{
				policyContainer = existingContainer;
			}
			else
			{
				policyContainer = new PolicyContainer(controllerName, actionName, _isAuthenticatedFunction, _rolesFunction);
				_itemValues.Add(policyContainer);
			}

			return policyContainer;
		}

		public void RemovePoliciesFor<TController>(Expression<Func<TController, object>> propertyExpression) where TController : Controller
		{
			var controllerName = typeof(TController).GetControllerName();
			var actionName = propertyExpression.GetActionName();

			var policyContainer = _itemValues.GetContainerFor(controllerName, actionName);
			if (policyContainer != null)
			{
				_itemValues.Remove(policyContainer);
			}
		}

		public void GetAuthenticationStatusFrom(Func<bool> isAuthenticatedFunction)
		{
			if (isAuthenticatedFunction == null)
				throw new ArgumentNullException("isAuthenticatedFunction");

			_isAuthenticatedFunction = isAuthenticatedFunction;
		}

		public void GetRolesFrom(Func<object[]> rolesFunction)
		{
			if (rolesFunction == null)
				throw new ArgumentNullException("rolesFunction");

			if (_itemValues.Count > 0)
				throw new ConfigurationErrorsException("You must set the rolesfunction before adding policies.");

			_rolesFunction = rolesFunction;
		}

		public void IgnoreMissingConfiguration()
		{
			ShouldIgnoreMissingConfiguration = true;
		}

		public bool ShouldIgnoreMissingConfiguration { get; private set; }
	}
}