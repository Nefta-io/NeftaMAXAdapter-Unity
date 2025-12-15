//
//  NeftaAdapter.m
//  UnityFramework
//
//  Created by Tomaz Treven on 18/11/2023.
//

#import <Foundation/Foundation.h>
#import <WebKit/WebKit.h>
#import <NeftaSDK/NeftaSDK-Swift.h>
#import <ALNeftaMediationAdapter.h>

#ifdef __cplusplus
extern "C" {
#endif
    typedef void (*OnReady)(const char *initConfig);
    typedef void (*OnInsights)(int requestId, int adapterResponseType, const char *adapterResponse);

    void EnableLogging(bool enable);
    void NeftaPlugin_SetExtraParameter(const char *key, const char *value);
    void NeftaPlugin_Init(const char *appId, OnReady onReady, OnInsights onInsights);
    void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload);
    void NeftaPlugin_OnExternalMediationRequest(const char *provider, int adType, const char *id0, const char *requestedAdUnitId, double requestedFloorPrice, int requestId);
    void NeftaPlugin_OnExternalMediationResponseAsString(const char *provider, const char *id0, const char *id2, double revenue, const char *precision, int status, const char *providerStatus, const char *networkStatus, const char *baseString);
    void NeftaPlugin_OnExternalMediationImpressionAsString(bool isClick, const char *provider, const char *data, const char *id0, const char *id2);
    const char * NeftaPlugin_GetNuid(void *instance, bool present);
    void NeftaPlugin_SetTracking(bool isAuthorized);
    void NeftaPlugin_SetContentRating(const char *rating);
    void NeftaPlugin_GetInsights(int requestId, int insights, int previousRequestId, int timeoutInSeconds);
    void NeftaPlugin_SetOverride(const char *root);
#ifdef __cplusplus
}
#endif

NeftaPlugin *_plugin;

void NeftaPlugin_EnableLogging(bool enable) {
    [NeftaPlugin EnableLogging: enable];
}

void NeftaPlugin_SetExtraParameter(const char *key, const char *value) {
    NSString *k = key ? [NSString stringWithUTF8String: key] : nil;
    NSString *v = value ? [NSString stringWithUTF8String: value] : nil;
    [NeftaPlugin SetExtraParameterWithKey: k value: v];
}

void NeftaPlugin_Init(const char *appId, OnReady onReady, OnInsights onInsights) {
    _plugin = [NeftaPlugin InitWithAppId: [NSString stringWithUTF8String: appId] integration: @"unity-applovin-max"];
    _plugin.OnReadyAsString = ^void(NSString * _Nullable initConfig) {
        const char *iC = initConfig ? [initConfig UTF8String] : NULL;
        onReady(iC);
    };
    _plugin.OnInsightsAsString = ^void(NSInteger requestId, NSInteger adapterResponseType, NSString * _Nullable adapterResponse) {
        const char *aR = adapterResponse ? [adapterResponse UTF8String] : NULL;
        onInsights((int)requestId, (int)adapterResponseType, aR);
    };
}

void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload) {
    NSString *n = name ? [NSString stringWithUTF8String: name] : nil;
    NSString *cp = customPayload ? [NSString stringWithUTF8String: customPayload] : nil;
    [_plugin RecordWithType: type category: category subCategory: subCategory name: n value: value customPayload: cp];
}

void NeftaPlugin_OnExternalMediationRequest(const char *provider, int adType, const char *id0, const char *requestedAdUnitId, double requestedFloorPrice, int requestId) {
    NSString *p = provider ? [NSString stringWithUTF8String: provider] : nil;
    NSString *i = id0 ? [NSString stringWithUTF8String: id0] : nil;
    NSString *rAI = requestedAdUnitId ? [NSString stringWithUTF8String: requestedAdUnitId] : nil;
    [NeftaPlugin OnExternalMediationRequest: p adType: adType id: i requestedAdUnitId: rAI requestedFloorPrice: requestedFloorPrice requestId: requestId];
}

void NeftaPlugin_OnExternalMediationResponseAsString(const char *provider, const char *id0, const char *id2, double revenue, const char *precision, int status, const char *providerStatus, const char *networkStatus, const char *baseString) {
    NSString *p = provider ? [NSString stringWithUTF8String: provider] : nil;
    NSString *i = id0 ? [NSString stringWithUTF8String: id0] : nil;
    NSString *i2 = id2 ? [NSString stringWithUTF8String: id2] : nil;
    NSString *pr = precision ? [NSString stringWithUTF8String: precision] : nil;
    NSString *pS = providerStatus ? [NSString stringWithUTF8String: providerStatus] : nil;
    NSString *nS = networkStatus ? [NSString stringWithUTF8String: networkStatus] : nil;
    NSString *bS = baseString ? [NSString stringWithUTF8String: baseString] : nil;
    [NeftaPlugin OnExternalMediationResponseAsString: p id: i id2: i2 revenue: revenue precision: pr status: status providerStatus: pS networkStatus: nS baseString: bS];
}

void NeftaPlugin_OnExternalMediationImpressionAsString(bool isClick, const char *provider, const char *data, const char *id0, const char *id2) {
    NSString *p = provider ? [NSString stringWithUTF8String: provider] : nil;
    NSString *d = data ? [NSString stringWithUTF8String: data] : nil;
    NSString *i = id0 ? [NSString stringWithUTF8String: id0] : nil;
    NSString *i2 = id2 ? [NSString stringWithUTF8String: id2] : nil;
    [NeftaPlugin OnExternalMediationImpressionAsString: isClick provider: p data: d id: i id2: i2];
}

const char * NeftaPlugin_GetNuid(void *instance, bool present) {
    const char *string = [[_plugin GetNuidWithPresent: present] UTF8String];
    char *returnString = (char *)malloc(strlen(string) + 1);
    strcpy(returnString, string);
    return returnString;
}

void NeftaPlugin_SetTracking(bool isAuthorized) {
	[_plugin SetTrackingWithIsAuthorized: isAuthorized];
}

void NeftaPlugin_SetContentRating(const char *rating) {
    [_plugin SetContentRatingWithRating: [NSString stringWithUTF8String: rating]];
}

void NeftaPlugin_GetInsights(int requestId, int insights, int previousRequestId, int timeoutInSeconds) {
    [_plugin GetInsightsBridge: requestId insights: insights previousRequestId: previousRequestId timeout: timeoutInSeconds];
}

void NeftaPlugin_SetOverride(const char *root) {
    [NeftaPlugin SetOverrideWithUrl: [NSString stringWithUTF8String: root]];
}