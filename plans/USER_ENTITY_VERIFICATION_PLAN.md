# 📋 SponsorPulse User Entity Verification & Testing Plan

**Project**: SponsorPulse MVP  
**Date**: April 15, 2026  
**Status**: PLANNING PHASE  
**Mode**: Architect → Code (pending validation)

---

## 📊 Executive Summary

This plan verifies the `ApplicationUser` entity implementation against MVP backlog requirements and establishes a comprehensive testing strategy for all untested user-centric use cases. The focus is on ensuring Twitter/Twitch data collection capabilities and proper entity relationships.

---

## 🔍 PHASE 1: Audit & Verification

### 1.1 ApplicationUser Domain Analysis

**Current Implementation** ([`ApplicationUser.cs`](../SponsorPulse/Domain/Entities/ApplicationUser.cs)):
```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? OrganizationName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? OrganizationLogoUrl { get; set; }
    public Settings? Settings { get; set; }
    public Dashboard? Dashboard { get; set; }
    public ICollection<Event> Events { get; set; }
    public ICollection<TwitchAuthToken> TwitchAuthTokens { get; set; }
}
```

**Verification Results**:
- ✅ **FirstName/LastName**: Present for user identification
- ✅ **OrganizationName**: Supports event organizer metadata
- ✅ **ProfilePhotoUrl/OrganizationLogoUrl**: Media storage ready
- ✅ **Events collection**: 1-to-Many relationship established
- ✅ **Dashboard**: 1-to-1 relationship configured
- ✅ **Settings**: 1-to-1 relationship configured
- ✅ **TwitchAuthTokens**: Multi-token support for OAuth

**MVP Alignment**:
- ✅ User can store personal data
- ✅ User can associate multiple events
- ✅ User has Settings entity for preferences
- ✅ User has Dashboard for overview
- ✅ OAuth tokens stored for Twitch integration

### 1.2 Entity Relationships Verification

**Database Context Configuration** ([`SponsorPulseDbContext.cs`](../SponsorPulse/Infrastructure/Persistence/SponsorPulseDbContext.cs)):

| Relationship | Configuration | Status |
|---|---|---|
| User ↔ Event | HasMany(Events), HasOne(Owner), FK: OwnerId | ✅ Correct |
| User ↔ Dashboard | HasOne(Dashboard), FK: UserId, 1:1 | ✅ Correct |
| User ↔ Settings | HasOne(Settings), FK: UserId, 1:1 | ✅ Correct |
| User ↔ TwitchAuthToken | HasMany(TwitchAuthTokens), FK: UserId | ✅ Correct |
| Event ↔ Media | HasMany(Media), Cascade Delete | ✅ Correct |
| Event ↔ TwitterAnalytics | JSON Column stored as TEXT | ✅ Correct |

**Global Query Filter**: Events are automatically filtered by CurrentUserId to ensure user isolation.

### 1.3 Twitch Integration Points

**Required Fields for Twitch Data Collection**:
- ✅ `TwitchAuthToken` entity stores: `UserId`, `AccessToken`, `State`, timestamps
- ✅ `Event` entity stores: `ViewerCount`, `PeakViewers`, `StreamDuration`, `StartedAt`, `GameName`
- ✅ `TwitchInfrastructureService` implements: GetStreamMetricsAsync, analytics endpoints
- ✅ OAuth flow configured in Program.cs with TwitchAuthEndpoints

**Service Interface** ([`ITwitchService.cs`](../SponsorPulse/Application/Common/Interfaces/ITwitchService.cs)):
- GetStreamMetricsAsync() → Returns TwitchMetrics with all required fields
- GetExtensionAnalyticsAsync() → CSV data for sponsor analytics
- GetGameAnalyticsAsync() → Game-specific analytics

### 1.4 Twitter Integration Points

**Required Fields for Twitter Data Collection**:
- ✅ `Event.TwitterHashtags` field present
- ✅ `Event.TwitterAnalytics` JSON model for storing results
- ⚠️ **TwitterAnalytics model**: Needs verification in code

**Service Interface** ([`ITwitterService.cs`](../SponsorPulse/Application/Common/Interfaces/ITwitterService.cs)):
- GetTwitterMetricsAsync() → Returns TwitterAnalytics for hashtag analysis
- Uses Xpoz.ai for data retrieval

---

## 🔐 PHASE 2: Authentication Use Cases

### 2.1 Account Creation
**Endpoint**: POST /login (Registration tab)  
**Status**: ✅ Implemented & Partially Tested

**Test Scenario**:
```
1. User fills registration form:
   - FirstName: "Jean"
   - LastName: "Dupont"
   - Email: "jean@example.com"
   - Password: "SecurePass123"
   - Organization: "DupontEsports"
2. System creates ApplicationUser via UserManager
3. User is auto-signed-in post-registration
4. User redirected to /dashboard
5. Dashboard + Settings initialized
```

**Expected Database State**:
- ApplicationUser created with Identity
- Settings entity created (1:1)
- Dashboard entity created (1:1)
- Email confirmed during registration

### 2.2 Account Login
**Endpoint**: POST /login (Login tab)  
**Status**: ✅ Implemented & Partially Tested

**Test Scenario**:
```
1. User enters email + password
2. SignInManager validates credentials
3. User principal created with NameIdentifier claim
4. Session persisted (8-hour expiry per config)
5. User redirected to /dashboard
```

**Session Validation**:
- AuthenticationStateProvider provides claims
- NameIdentifier or "sub" claim extracted as UserId
- Dashboard filters events by this UserId

### 2.3 Dashboard Initialization
**Component**: [`Dashboard.razor`](../SponsorPulse/Presentation/Pages/Dashboard.razor)  
**Status**: ✅ Implemented, Setup Verified

**Initialization Flow**:
1. Authentication check via @attribute [Authorize]
2. LoadEvents() → Filters by CurrentUserId
3. LoadDashboardStats() → Aggregate metrics
4. EventSelector renders user's events

---

## 📌 PHASE 3: Event Management Use Cases

### 3.1 Create Event
**Component**: [`CreateEvent.razor`](../SponsorPulse/Presentation/Pages/CreateEvent.razor)  
**Command**: [`CreateEventCommand.cs`](../SponsorPulse/Application/Events/Commands/CreateEventCommand.cs)  
**Status**: ✅ Implemented

**Test Scenario**:
```
Input Fields:
- Name: "Grand Tournoi Rocket League 2026"
- Description: "Tournoi national avec sponsors"
- Date: 2026-04-20
- StreamPlatform: "Twitch"
- ChannelId: "gotaga"
- TwitterHashtags: "#RocketLeague;#esports;#TourneeXRL"

Execution:
1. Validation via DataAnnotations
2. Event.Create() generates GUID v7 ID and slug
3. Event.OwnerId = CurrentUserId
4. EF Core saves to Events table
5. Modal closes, Dashboard refreshes
```

**Data Validation**:
- ✅ Name: Required, max 100 chars
- ✅ Description: Required
- ✅ Date: Required
- ✅ Platform: Required (Twitch/YouTube)
- ✅ ChannelId: Required, used for metrics fetch
- ✅ TwitterHashtags: Optional, semicolon-delimited

### 3.2 Modify Event
**Status**: ⚠️ NOT FULLY IMPLEMENTED

**Missing Implementation**:
- No edit endpoint/component exists
- Dashboard references `HandleEditEvent()` → navigates to `/event/{slug}/metrics`
- EventDetails page may need edit capability

**Required Development**:
```csharp
// Application Layer
public class UpdateEventCommand { ... }

// Presentation
// EventDetails.razor → Edit mode toggle
// SaveChanges → EventService.UpdateEventAsync()
```

### 3.3 Delete Event
**Component**: [`Dashboard.razor`](../SponsorPulse/Presentation/Pages/Dashboard.razor)  
**Status**: ✅ Implemented

**Test Scenario**:
```
1. User clicks delete on event card
2. Confirmation modal appears
3. User confirms deletion
4. Event record deleted via EF Core
5. Cascade delete applies to Media, TwitchAuthTokens
6. Dashboard refreshes
```

**Safety Checks**:
- ✅ User ownership verified via idClaim
- ✅ Cascade delete configured in DbContext
- ✅ Query filter prevents cross-user access

---

## 🐦 PHASE 4: Twitter Data Collection

### 4.1 Service Architecture
**Interface**: [`ITwitterService.cs`](../SponsorPulse/Application/Common/Interfaces/ITwitterService.cs)  
**Implementation**: `TwitterInfrastructureService` (location: TBD)

**Method Signature**:
```csharp
Task<Result<TwitterAnalytics>> GetTwitterMetricsAsync(
    string query,
    DateTime startDate,
    DateTime endDate,
    CancellationToken cancellationToken = default
);
```

### 4.2 Data Model
**Domain Model**: `TwitterAnalytics` (location: TBD)  
**Storage**: Event.TwitterAnalytics (JSON column in TEXT)

**Expected Fields** (to verify):
- query: Original search query
- impressions: Total impression count
- engagements: Likes + retweets + replies
- sentiment: Positive/Neutral/Negative breakdown
- topMentions: Most mentioned accounts
- peakEngagement: Peak engagement timestamp
- period: [startDate, endDate]

### 4.3 Test Scenarios

**Scenario A: Standard Hashtag Search**
```
1. Event created with TwitterHashtags: "#RocketLeague"
2. User triggers analysis
3. Service queries Xpoz.ai for 7-day metrics
4. TwitterAnalytics stored in Event entity
5. Report displays metrics
```

**Scenario B: Multi-Hashtag Search**
```
1. TwitterHashtags: "#RocketLeague;#esports;#TourneeXRL"
2. Service aggregates results across all queries
3. Combined TwitterAnalytics generated
4. Report shows consolidated view
```

**Scenario C: Manual Data Entry Fallback**
```
1. Twitter API unavailable
2. User manually enters metrics
3. TwitterAnalytics manually populated
4. Report continues with user-provided data
```

---

## 🎮 PHASE 5: Twitch Data Collection

### 5.1 Service Architecture
**Interface**: [`ITwitchService.cs`](../SponsorPulse/Application/Common/Interfaces/ITwitchService.cs)  
**Implementation**: [`TwitchInfrastructureService.cs`](../SponsorPulse/Infrastructure/Services/TwitchInfrastructureService.cs) ✅

**Primary Method**:
```csharp
Task<Result<TwitchMetrics>> GetStreamMetricsAsync(
    string channelNameOrUrl,
    string? userAccessToken = null
);
```

### 5.2 Data Collection Methods

| Method | Purpose | Status |
|---|---|---|
| GetStreamMetricsAsync | Fetch live/VOD metrics | ✅ Implemented |
| GetExtensionAnalyticsCsvUrl | Extension performance | ✅ Implemented |
| GetGameAnalyticsCsvUrl | Game-specific data | ✅ Implemented |

**TwitchMetrics Fields Collected**:
- ViewerCount: Current or VOD view count
- PeakViewers: Maximum concurrent viewers
- StreamDuration: Duration.TimeSpan
- StartedAt: Stream start timestamp
- GameName: Game being streamed

### 5.3 Authentication Flow
**OAuth Provider**: Twitch OAuth 2.0  
**Grant Types**:
- Client Credentials (app-level analytics)
- Authorization Code (user-level analytics)

**Token Storage**:
```csharp
public class TwitchAuthToken {
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccessToken { get; set; }
    public string State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

**Endpoints Configured**:
- MapTwitchAuthEndpoints() → OAuth callback
- MapTwitchAnalyticsEndpoints() → Fetch data

### 5.4 Test Scenarios

**Scenario A: Stream Metrics via Channel Name**
```
Input: "gotaga" (Twitch channel login)
1. Service calls /users?login=gotaga
2. Resolves user_id
3. Checks /streams?user_id={id} for LIVE stream
4. Returns TwitchMetrics with current viewers
5. Stored in Event entity
```

**Scenario B: VOD Metrics Fallback**
```
Input: Channel with no LIVE stream
1. Service falls back to /videos?user_id={id}&first=1&sort=time
2. Returns latest VOD metrics
3. ViewCount mapped to ViewerCount
4. PeakViewers set to 0 (not available for VOD)
```

**Scenario C: Video ID Direct Lookup**
```
Input: "123456789" (Twitch video ID) or full URL
1. Service detects video ID format
2. Queries /videos?id=123456789
3. Returns VOD metrics directly
```

**Scenario D: OAuth User Analytics**
```
1. User completes Twitch OAuth flow
2. AccessToken stored in TwitchAuthToken
3. Service calls analytics endpoints with user token
4. Returns extension/game analytics as CSV
5. CSV parsed and stored
```

---

## 📄 PHASE 6: Report Generation

### 6.1 PDF Generation Flow
**Component**: [`EventPdfCustomize.razor`](../SponsorPulse/Presentation/Pages/EventPdfCustomize.razor)  
**Service**: [`PdfGenerationService.cs`](../SponsorPulse/Application/Services/PdfGenerationService.cs)

**Current Status**: ✅ Templates exist, flow partially tested

**Test Scenario**:
```
1. User navigates to /event/{slug}/pdf-customize
2. Page loads Event with Twitter/Twitch data
3. User customizes colors, branding
4. User triggers PDF generation
5. Service renders template → PDF
6. PDF uploaded to Cloudflare R2
7. Download link provided
```

### 6.2 Data Integration in Reports
**Report Templates** (4 variants):
- [`SponsorReport.razor`](../SponsorPulse/Presentation/Templates/SponsorReport.razor)
- `SponsorReport2.razor`
- `SponsorReport3.razor`
- `SponsorReport4.razor`

**Data Injected**:
- Event name, date, description
- TwitchMetrics: viewers, duration, game
- TwitterAnalytics: impressions, engagement, sentiment
- Media: uploaded photos from Cloudflare R2
- User Organization branding

### 6.3 Media Storage Integration
**Service**: [`MediaStorageService.cs`](../SponsorPulse/Infrastructure/Services/MediaStorageService.cs)  
**Storage**: Cloudflare R2

**Components Involved**:
- [`MediaUploader.razor`](../SponsorPulse/Presentation/Components/Media/MediaUploader.razor)
- [`MediaPresignedUrlExtensions.cs`](../SponsorPulse/Infrastructure/Api/Extensions/MediaPresignedUrlExtensions.cs)

**Flow**:
1. User uploads image via MediaUploader
2. PresignedUrl endpoint generates upload URL
3. Client uploads directly to R2
4. URL stored in EventMedia entity
5. Report template references URL

---

## ⚙️ PHASE 7: Bug Fixes & Refinement

### 7.1 Error Handling Audit
**Standard**: Problem Details Format (RFC 7807)

**Current Status**: ✅ Partially implemented
- TwitchInfrastructureService returns Result<T>
- Need to verify Problem Details in API endpoints

### 7.2 Clean Architecture Compliance
**Verification Points**:
- ✅ Domain: Event, ApplicationUser, Dashboard entities
- ✅ Application: Services, Commands, Interfaces
- ✅ Infrastructure: Repositories, DbContext, External services
- ✅ Presentation: Blazor pages, components

**Potential Issues**:
- ⚠️ Verify no business logic in Razor pages
- ⚠️ Check for circular dependencies
- ⚠️ Ensure DI configuration complete

### 7.3 Code Quality
**Points to Review**:
- No magic strings/numbers
- Async/await consistent usage
- Pure functions where applicable
- GUID v7 for all IDs
- Proper null handling

---

## 📋 Testing Checklist

### Authentication Tests
- [ ] User registration with validation
- [ ] User login with correct credentials
- [ ] Login failure with wrong password
- [ ] Session persistence across navigation
- [ ] Logout and session termination
- [ ] Dashboard accessible only when authenticated

### Event Management Tests
- [ ] Create event with all fields
- [ ] Create event with minimal fields (Twitter optional)
- [ ] Modify event details
- [ ] Delete event with confirmation
- [ ] User cannot access other users' events
- [ ] Event list filters by current user

### Twitter Data Tests
- [ ] Fetch metrics for valid hashtag
- [ ] Fetch metrics for multiple hashtags
- [ ] Handle API failure gracefully
- [ ] Store TwitterAnalytics in Event
- [ ] Display Twitter metrics in report

### Twitch Data Tests
- [ ] Fetch metrics for valid channel name
- [ ] Fetch metrics for video URL
- [ ] Fallback to VOD when stream offline
- [ ] Store TwitchMetrics in Event
- [ ] Display Twitch metrics in report
- [ ] OAuth token stored and reused

### Report Generation Tests
- [ ] Generate PDF with Twitter data
- [ ] Generate PDF with Twitch data
- [ ] Generate PDF with both datasets
- [ ] Upload PDF to Cloudflare R2
- [ ] Provide download link
- [ ] Test all 4 report templates

### Integration Tests
- [ ] Complete user flow: Create → Fetch data → Generate report
- [ ] User isolation: Cannot see other users' data
- [ ] Cascade delete: Deleting user deletes dependent entities
- [ ] Transaction rollback on error

---

## 🎯 Execution Strategy

### Step 1: Fix Missing Implementations
1. Event edit functionality (UpdateEventCommand, UI)
2. Twitter service implementation (if not yet done)
3. Dashboard/Settings initialization on user creation
4. Report templates data binding

### Step 2: Integration Testing
1. Test each use case manually
2. Verify database state after each operation
3. Check API responses format
4. Validate error scenarios

### Step 3: Refinement
1. Fix bugs discovered during testing
2. Add missing error handling
3. Optimize data queries
4. Improve UX/validation messages

### Step 4: Documentation
1. API endpoint documentation
2. User journey diagrams
3. Database schema documentation
4. OAuth flow documentation

---

## 📊 Expected Deliverables

Upon completion, the following should be fully functional:

1. ✅ **User Management**
   - Account creation with validation
   - Login/logout with session management
   - User profile data storage

2. ✅ **Event Lifecycle**
   - Create, read, update, delete events
   - User ownership isolation
   - Event metadata (date, platform, channel)

3. ✅ **Data Collection**
   - Twitter hashtag analysis
   - Twitch stream metrics
   - Media uploads to cloud storage

4. ✅ **Report Generation**
   - PDF templates with dynamic data
   - Custom branding support
   - Cloud storage integration

5. ✅ **Quality Assurance**
   - Clean Architecture compliance
   - Error handling with Problem Details
   - User isolation verification
   - End-to-end integration tests

---

## ⚠️ Known Issues & Risks

| Issue | Severity | Status | Action |
|---|---|---|---|
| Event edit UI missing | Medium | TBD | Implement UpdateEvent flow |
| Twitter service impl unclear | Medium | TBD | Verify implementation exists |
| Dashboard init on signup | Low | TBD | Verify automatic creation |
| Report data integration | Medium | TBD | Test with real Twitch/Twitter data |

---

## 📞 Next Steps

1. **Review this plan** - Validate architecture and requirements
2. **Approve plan** - Confirm all phases and scope
3. **Switch to Code mode** - Begin implementation/fixes
4. **Execute Phase 1-7** - Follow testing checklist
5. **Validation** - Confirm all use cases working

---

**Plan Version**: 1.0  
**Last Updated**: 2026-04-15  
**Status**: Awaiting Approval ⏳
