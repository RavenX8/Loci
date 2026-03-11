using LociApi.Enums;

namespace Loci.Services.Mediator;

// Calls respective to API invokes.
// Most of these should be samethreadmessages unless not high priority.

// LociApiBase
public record NewEnabledStateMessage(bool NewState) : SameThreadMessage;

// LociApiManager
public record ActorSMChanged(IntPtr Address) : SameThreadMessage;
public record ActorSMStatusesChanged(IntPtr Address, Guid StatusId, StatusChangeType Change) : SameThreadMessage;
public record ApplyToTargetMessage(IntPtr TargetAddress, string TargetHost, List<LociStatusInfo> Data) : SameThreadMessage;

// LociApiStatuses
public record StatusModifiedMessage(Guid StatusId, bool WasDeleted) : SameThreadMessage;
public record ChainTriggerHitMessage(IntPtr Address, Guid StatusId, ChainTrigger Trigger, ChainType ChainType, Guid ChainedId) : SameThreadMessage;

// LociApiPresets
public record PresetModifiedMessage(Guid PresetId, bool WasDeleted) : SameThreadMessage;

// LociApiEvents
public record EventModifiedMessage(Guid EventId, bool WasDeleted) : SameThreadMessage;
public record EventPathMoved(Guid EventId, string OldPath, string NewPath) : SameThreadMessage;
