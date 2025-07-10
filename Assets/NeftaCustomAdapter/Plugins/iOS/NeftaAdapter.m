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
    typedef void (*OnInsights)(int requestId, const char *insights);

    void EnableLogging(bool enable);
    void NeftaPlugin_Init(const char *appId, OnInsights onInsights);
    void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload);
    void NeftaPlugin_OnExternalMediationRequest(const char *mediationProvider, int adType, const char *recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, const char *adUnitId, double revenue, const char *precision, int status, const char *providerStatus, const char *networkStatus);
    void NeftaAdapter_OnExternalMediationImpressionAsString(const char *network, const char *format, const char *creativeId, const char *data, double revenue, const char *precision);
    void NeftaPlugin_OnExternalMediationImpressionAsString(const char *mediationProvider, const char *data, int adType, double revenue, const char *precision);
    const char * NeftaPlugin_GetNuid(void *instance, bool present);
    void NeftaPlugin_SetContentRating(const char *rating);
    void NeftaPlugin_GetInsights(int requestId, int insights, int timeoutInSeconds);
    void NeftaPlugin_SetOverride(const char *root);
#ifdef __cplusplus
}
#endif

NeftaPlugin *_plugin;

void NeftaPlugin_EnableLogging(bool enable) {
    [NeftaPlugin EnableLogging: enable];
}

void NeftaPlugin_Init(const char *appId, OnInsights onInsights) {
    _plugin = [NeftaPlugin InitWithAppId: [NSString stringWithUTF8String: appId]];
    _plugin.OnInsightsAsString = ^void(NSInteger requestId, NSString * _Nullable insights) {
        const char *cBI = insights ? [insights UTF8String] : NULL;
        onInsights((int)requestId, cBI);
    };
}

void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload) {
    NSString *n = name ? [NSString stringWithUTF8String: name] : nil;
    NSString *cp = customPayload ? [NSString stringWithUTF8String: customPayload] : nil;
    [_plugin RecordWithType: type category: category subCategory: subCategory name: n value: value customPayload: cp];
}

void NeftaPlugin_OnExternalMediationRequest(const char *mediationProvider, int adType, const char *recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, const char *adUnitId, double revenue, const char *precision, int status, const char *providerStatus, const char *networkStatus) {
    NSString *mP = mediationProvider ? [NSString stringWithUTF8String: mediationProvider] : nil;
    NSString *r = recommendedAdUnitId ? [NSString stringWithUTF8String: recommendedAdUnitId] : nil;
    NSString *a = adUnitId ? [NSString stringWithUTF8String: adUnitId] : nil;
    NSString *p = precision ? [NSString stringWithUTF8String: precision] : nil;
    NSString *pS = providerStatus ? [NSString stringWithUTF8String: providerStatus] : nil;
    NSString *nS = networkStatus ? [NSString stringWithUTF8String: networkStatus] : nil;
    [NeftaPlugin OnExternalMediationRequest: mP adType: adType recommendedAdUnitId: r requestedFloorPrice: requestedFloorPrice calculatedFloorPrice: calculatedFloorPrice adUnitId: a revenue: revenue precision: p status: status providerStatus: pS networkStatus: nS];
}

void NeftaAdapter_OnExternalMediationImpressionAsString(const char *network, const char *format, const char *creativeId, const char *data, double revenue, const char *precision) {
    NSString *n = network ? [NSString stringWithUTF8String: network] : nil;
    NSString *f = format ? [NSString stringWithUTF8String: format] : nil;
    NSString *c = creativeId ? [NSString stringWithUTF8String: creativeId] : nil;
    NSString *d = data ? [NSString stringWithUTF8String: data] : nil;
    NSString *p = precision ? [NSString stringWithUTF8String: precision] : nil;
    [ALNeftaMediationAdapter OnExternalMediationImpressionAsString: n format: f creativeId: c data: d revenue: revenue precision: p];
}

void NeftaPlugin_OnExternalMediationImpressionAsString(const char *mediationProvider, const char *data, int adType, double revenue, const char *precision) {
    NSString *mP = mediationProvider ? [NSString stringWithUTF8String: mediationProvider] : nil;
    NSString *d = data ? [NSString stringWithUTF8String: data] : nil;
    NSString *p = precision ? [NSString stringWithUTF8String: precision] : nil;
    [NeftaPlugin OnExternalMediationImpressionAsString: mP data: d adType: adType revenue: revenue precision: p];
}

void NeftaPlugin_SetContentRating(const char *rating) {
    [_plugin SetContentRatingWithRating: [NSString stringWithUTF8String: rating]];
}

const char * NeftaPlugin_GetNuid(void *instance, bool present) {
    const char *string = [[_plugin GetNuidWithPresent: present] UTF8String];
    char *returnString = (char *)malloc(strlen(string) + 1);
    strcpy(returnString, string);
    return returnString;
}

void NeftaPlugin_GetInsights(int requestId, int insights, int timeoutInSeconds) {
    [_plugin GetInsightsBridge: requestId insights: insights timeout: timeoutInSeconds];
}

void NeftaPlugin_SetOverride(const char *root) {
    [NeftaPlugin SetOverrideWithUrl: [NSString stringWithUTF8String: root]];
}