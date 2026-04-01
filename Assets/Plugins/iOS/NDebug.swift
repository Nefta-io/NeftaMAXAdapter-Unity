//
//  NDebug.swift
//  DirectIntegration
//
//  Created by Tomaz Treven on 13. 8. 24.
//

import Foundation
import Network
import NeftaSDK
import UIKit
import OSLog

@objc public class NDebug : NSObject {
    
    static var _title: String?
    static var _onClick: (() -> Void)? = nil
    static var _onClose: (() -> Void)? = nil
    static var _onReward: (() -> Void)? = nil
    
    @objc public static func Open(title: String,
                                   viewController: UIViewController,
                                   onShow: @escaping () -> Void,
                                   onClick: @escaping () -> Void,
                                   onClose: @escaping () -> Void,
                                   onReward: (() -> Void)? = nil) {
        
        _title = title
        _onClick = onClick
        _onClose = onClose
        _onReward = onReward
        
        onShow()
        
        let adViewController = AdViewController()
        adViewController.modalPresentationStyle = .fullScreen
        viewController.present(adViewController, animated: false, completion: nil)
    }
}

public class AdViewController : UIViewController {
    
    private var delayedTask: DispatchWorkItem?
    
    init() {
        super.init(nibName: nil, bundle: nil)
    }
    
    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
    }
    
    public override func viewDidLoad() {
        view.backgroundColor = .white
                
        let screenBounds = UIScreen.main.bounds
        
        let titleLabel = UILabel()
        titleLabel.text = NDebug._title!
        titleLabel.font = UIFont.systemFont(ofSize: 24, weight: .bold)
        titleLabel.textAlignment = .center
        titleLabel.sizeToFit()
        let labelWidth: CGFloat = 300
        titleLabel.frame = CGRect(
            x: (screenBounds.width - labelWidth) / 2,
            y: 80 + view.safeAreaInsets.top,
            width: labelWidth,
            height: titleLabel.frame.height
        )
        view.addSubview(titleLabel)
    
        let closeButton = UIButton(type: .system)
        closeButton.setTitle("X", for: .normal)
        closeButton.titleLabel?.font = UIFont.systemFont(ofSize: 18)
        closeButton.backgroundColor = .systemRed
        closeButton.setTitleColor(.white, for: .normal)
        closeButton.layer.cornerRadius = 8
        closeButton.frame = CGRect(
            x: screenBounds.width - 80,
            y: 40 + view.safeAreaInsets.top,
            width: 40,
            height: 40
        )
        
        view.addSubview(closeButton)
        
        let centerButton = UIButton(type: .system)
        centerButton.setTitle("Ad", for: .normal)
        centerButton.titleLabel?.font = UIFont.systemFont(ofSize: 28)
        centerButton.backgroundColor = .systemBlue
        centerButton.setTitleColor(.white, for: .normal)
        centerButton.layer.cornerRadius = 16
        centerButton.frame = CGRect(
            x: (screenBounds.width - 300) / 2,
            y: (screenBounds.height - 400) / 2,
            width: 300,
            height: 400
        )
        
        view.addSubview(centerButton)
        
        closeButton.addTarget(self, action: #selector(closeTapped), for: .touchUpInside)
        centerButton.addTarget(self, action: #selector(centerTapped), for: .touchUpInside)
    }
    
    @objc private func closeTapped() {
        NDebug._onClose!()
        dismiss(animated: false, completion: nil)
    }
    
    @objc private func centerTapped() {
        NDebug._onClick!()
    }
    
    public override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)
        
        delayedTask?.cancel()
        
        let task = DispatchWorkItem { [weak self] in
            guard let self = self else { return }
            if self.isBeingDismissed || self.isMovingFromParent || self.view.window == nil {
                return
            }
            if let onReward = NDebug._onReward {
                onReward()
            }
        }
        delayedTask = task
        DispatchQueue.main.asyncAfter(deadline: .now() + 3.0, execute: task)
    }
    
    public override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        
        delayedTask?.cancel()
        delayedTask = nil
    }
}

@objc class DebugServer : NSObject {

    private let _broadcastPort = NWEndpoint.Port(rawValue: 12010)
    
    private var _viewController: UIViewController
    
    private var _name: String?
    private var _version: String?
    private var _bundleId: String?
    
    private var _broadcastIp: String?
    private var _listener: NWListener?
    private var _broadcastConnection: NWConnection?
    private var _localPort: UInt16 = 0
    private var _timer: Timer?
    private var _lastLogTime: Int = 0
    private var _store: OSLogStore?
    private var _predicate: NSPredicate?
    
    private static var _instance: DebugServer?
    
    @objc public static func Init(viewController: UIViewController) {
        if _instance == nil {
            _instance = DebugServer(viewController: viewController)
        }
    }
    
    init(viewController: UIViewController) {
        _viewController = viewController
        super.init()

        _name = UIDevice.current.model
#if targetEnvironment(simulator)
        if let simModelCode = ProcessInfo().environment["SIMULATOR_MODEL_IDENTIFIER"] {
            _name = simModelCode
        }
#else
        var size: size_t = 0
        sysctlbyname("hw.machine", nil, &size, nil, 0)
        var machine = [CChar](repeating: 0, count: Int(size))
        sysctlbyname("hw.machine", &machine, &size, nil, 0)
        _name = String(cString: machine)
#endif
        
        _version = "0.0.0"
        if let bundleInfo = Bundle.main.infoDictionary {
            _version = "\(bundleInfo["CFBundleShortVersionString"]!).\(bundleInfo["CFBundleVersion"]!)"
        }
        _bundleId = Bundle.main.bundleIdentifier
        
        let arguments = ProcessInfo.processInfo.arguments
        if arguments.count > 1 {
            let overrideUrl = arguments[1]
            if  overrideUrl.count > 2 {
                NeftaPlugin.SetOverride(url: overrideUrl)
            }
        }
        
        NeftaPlugin._instance = nil
        NeftaPlugin.SetDebugTime(offset: 0)
        do {
            _store = try OSLogStore(scope: .currentProcessIdentifier)
            _predicate = NSPredicate(format: "NOT (subsystem BEGINSWITH[c] 'com.apple.')")
        } catch {
            print("DS:Error attaching to log stream: \(error)")
        }
        
        _broadcastIp = GetBroadcastAddress()
        if _broadcastIp == nil {
            print("DS:No wifi")
        } else {
            StartListening()
        }
    }
    
    deinit {
        print("DS:deinit")

        if _timer != nil {
            _timer!.invalidate()
            _timer = nil
        }
        
        if _broadcastConnection != nil {
            _broadcastConnection!.cancel()
            _broadcastConnection = nil
        }
        
        if _listener != nil {
            _listener!.cancel()
            _listener = nil
        }
    }
    
    private func Send() {
        guard let connection = self._broadcastConnection else {
            return
        }
        
        SendState(connection: connection, to: "master")
        
        do {
            let filter = NSCompoundPredicate(andPredicateWithSubpredicates: [
                NSPredicate(format: "date > %@", Date(timeIntervalSince1970: Double(_lastLogTime) / 1000) as NSDate),
                _predicate!
            ])
            let entries = try _store!.getEntries(matching: filter)
            let newLogs = entries.compactMap { $0 as? OSLogEntryLog }
            
            for log in newLogs {
                _lastLogTime = Int(log.date.timeIntervalSince1970 * 1000 + 1)
                var msg = "log|\(_lastLogTime)|\(log.composedMessage)"
                if msg.count > 1400 {
                    msg = String(msg.prefix(1400))
                }
                self.SendUdp(connection: connection, to: "master", message: msg)
            }
        } catch {
            print("DS try send logs error: \(error)")
        }
    }
    
    private func StartBroadcastServer() {
        let params = NWParameters.udp
        params.allowLocalEndpointReuse = true
        params.includePeerToPeer = true
        
        let endpoint = NWEndpoint.hostPort(host: NWEndpoint.Host(_broadcastIp!), port: _broadcastPort!)
        print("DS:Starting broadcast on: \(endpoint)")
        _broadcastConnection = NWConnection(to: endpoint, using: params)
        _broadcastConnection!.stateUpdateHandler = { state in
            switch state {
                case .ready:
                    self.SendState(connection: self._broadcastConnection!, to: "master")
                
                    DispatchQueue.main.async {
                        self._timer = Timer.scheduledTimer(withTimeInterval: 5.0, repeats: true) { _ in
                            self.Send()
                        }
                    }
                    self.Send()
                case .failed(let error):
                    print("DS:Broadcast failed on: \(error)")
                default:
                    break
            }
        }
        _broadcastConnection!.start(queue: .global())
    }
    
    private func SendUdp(connection: NWConnection, to: String, message: String) {
        let data = "\(self._name!)|\(to)|\(message)"
        connection.send(content: data.data(using: .utf8)!, completion: .contentProcessed { error in
            if let error = error {
                print("DS:Error sending broadcast: \(message) |: \(error.localizedDescription)")
            }
        })
    }
    
    func StartListening() {
        let parameters = NWParameters.udp
        parameters.allowLocalEndpointReuse = true
        _listener = try? NWListener(using: parameters)
        _listener!.stateUpdateHandler = { newState in
            switch newState {
            case .ready:
                if let port = self._listener?.port {
                    print("DS:Listening on port \(port)")
                    self._localPort = port.rawValue
                    self.StartBroadcastServer()
                }
            default:
                print("DS:Failed listening: \(newState)")
                break
            }
        }
        _listener!.newConnectionHandler = { newConnection in
            newConnection.start(queue: .global())
            self.ReceiveBroadcast(on: newConnection)
        }
        _listener!.start(queue: .global())
    }
    
    private func ReceiveBroadcast(on connection: NWConnection) {
        connection.receiveMessage { data, context, isComplete, error in
            if let error = error {
                print("DS:Error receiving broadcast: \(error.localizedDescription)")
                return
            }

            if let data = data, let message = String(data: data, encoding: .utf8) {
                print("DS:Received broadcast: \(message)")
                
                let segments = message.components(separatedBy: "|")
                let sourceName = segments[0]
                let control = segments[3]
                switch control {
                case "get_logs":
 
                    break
                case "set_time_offset":
                    let offsetString = segments[4]
                    if let offset = Int(offsetString) {
                        NeftaPlugin.SetDebugTime(offset: offset)
                    }
                    self.SendUdp(connection: connection, to: sourceName, message: "return|set_time_offset")
                    break
                case "state":
                    self.SendState(connection: connection, to: sourceName)
                    break
                case "add_event":
                    do {
                        var name : String?
                        var value: Int64 = 0
                        var customPayload : String?
                        if segments[4] == "progression" {
                            let status = self.ToProgressionStatus(segments[5])
                            let type = self.ToProgressionType(segments[6])
                            let source = self.ToProgressionSource(segments[7])
                            if segments.count > 8 {
                                name = segments[8]
                            }
                            if segments.count > 9 {
                                value = Int64(segments[9])!
                            }
                            if segments.count > 10 {
                                customPayload = segments[10]
                            }
                            NeftaPlugin._instance!.Events.AddProgressionEvent(status: status, type: type, source: source, name: name, value: value, customPayload: customPayload)
                        } else if segments[4] == "receive" {
                            let category = self.ToResourceCategory(segments[5])
                            let method = self.ToReceiveMethod(segments[6])
                            if (segments.count > 7) {
                                name = segments[7]
                            }
                            if segments.count > 8 {
                                value = Int64(segments[8])!
                            }
                            if segments.count > 9 {
                                customPayload = segments[9]
                            }
                            NeftaPlugin._instance!.Events.AddReceiveEvent(category: category, method: method, name: name, quantity: value, customPayload: customPayload)
                        } else if segments[4] == "spend" {
                            let category = self.ToResourceCategory(segments[5])
                            let method = self.ToSpendMethod(segments[6])
                            if (segments.count > 7) {
                                name = segments[7]
                            }
                            if segments.count > 8 {
                                value = Int64(segments[8])!
                            }
                            if segments.count > 9 {
                                customPayload = segments[9]
                            }
                            NeftaPlugin._instance!.Events.AddSpendEvent(category: category, method: method, name: name, quantity: value, customPayload: customPayload)
                            
                            NeftaPlugin._instance!.Events.AddSpendEvent(category: NeftaEvents.ResourceCategory.SoftCurrency, method: NeftaEvents.SpendMethod.Other, name: "coins", quantity: 5)
                        } else if segments[4] == "revenue" {
                            name = segments[5]
                            let price = Decimal(string: segments[6])!
                            let currency = segments[7]
                            if segments.count > 8 {
                                customPayload = segments[8]
                            }
                            NeftaPlugin._instance!.Events.AddPurchaseEvent(name: name!, price: price, currency: currency, customPayload: customPayload)
                        }
                        self.SendUdp(connection: connection, to: sourceName, message: "return|add_event")
                    }
                case "add_unity_event":
                    do {
                        let type = Int(segments[4])!
                        let category = Int(segments[5])!
                        let subCategory = Int(segments[6])!
                        let name = segments[7]
                        let value = Int64(segments[8])!
                        var customPayload: String? = nil
                        if segments.count > 9 {
                            customPayload = segments[9]
                        }
                        var repeatCount: Int64 = 1
                        if segments.count > 10 {
                            repeatCount = Int64(segments[10])!
                        }
                        
                        for _ in 0..<repeatCount {
                            DispatchQueue.global(qos: .background).async {
                                NeftaPlugin._instance!.Record(type: type, category: category, subCategory: subCategory, name: name, value: value, customPayload: customPayload)
                            }
                        }
                        
                        self.SendUdp(connection: connection, to: sourceName, message: "return|add_unity_event")
                    }
                case "add_external_mediation_request":
                    do {
                        let provider = segments[4]
                        let id1 = segments[5]
                        let id2 = segments[6]
                        let revenue = Float64(segments[8])!
                        let precision = segments[9]
                        let status = Int(segments[10])!
                        var providerStatus: String? = nil
                        if segments.count > 11 {
                            providerStatus = segments[11]
                        }
                        var networkStatus: String? = nil
                        if segments.count > 12 {
                            networkStatus = segments[12]
                        }
                        NeftaPlugin.OnExternalMediationResponse(provider, id: id1, id2: id2, revenue: revenue, precision: precision, status: status, providerStatus: providerStatus, networkStatus: networkStatus, baseObject: nil)
                        self.SendUdp(connection: connection, to: sourceName, message: "return|add_external_mediation_request")
                    }
                case "add_external_mediation_impression":
                    do {
                        let isClick = Bool(segments[4])!
                        let provider = segments[5]
                        let id0 = segments[6]
                        let id2 = segments[7]
                        NeftaPlugin.OnExternalMediationImpression(isClick, provider: provider, data: nil, id: id0, id2: id2)
                        self.SendUdp(connection: connection, to: sourceName, message: "return|add_external_mediation_impression")
                    }
                case "get_insights":
                    let insights = Int(segments[4])!
                    let callbackIndex = Int(segments[5])!
                    
                    NeftaPlugin._instance!.GetInsights(insights, previousInsight: nil, callback: { insights in
                        let timestamp = Int64(Date().timeIntervalSince1970 * 1000)
                        if let ii = insights._interstitial {
                            self.SendUdp(connection: connection, to: "master", message: "log|\(timestamp)|DS:Insights:\(ii._type),\(ii._adOpportunityId),\(ii._auctionId),\(ii._requestId),\(ii._adUnit ?? ""),\(ii._floorPrice),\(ii._delay)`")
                        }
                        if let ir = insights._rewarded {
                            self.SendUdp(connection: connection, to: "master", message: "log|\(timestamp)|DS:Insights\(ir._type),\(ir._adOpportunityId),\(ir._auctionId),\(ir._requestId),\(ir._adUnit ?? ""),\(ir._floorPrice),\(ir._delay)")
                        }
                    })
                    
                    self.SendUdp(connection: connection, to: sourceName, message: "return|get_insights|\(insights)|\(callbackIndex)")
                    break
                case "set_override":
                    let app_id = segments[4]
                    var rest_url: String? = segments[5]
                    if rest_url!.isEmpty && rest_url == "null" {
                        rest_url = nil
                    }
                    
                    NeftaPlugin._instance!._info._appId = app_id
                    NeftaPlugin.SetOverride(url: rest_url)
                    if segments.count > 6 && !segments[6].isEmpty {
                        NeftaPlugin._instance!._state._nuid = segments[6]
                    }
                    
                    self.SendUdp(connection: connection, to: sourceName, message: "return|set_override")
                    break
                case "create_file":
                    let path = segments[4]
                    let content = segments[5]
                    
                    var finalPath = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first!
                    do {
                        finalPath = finalPath.appendingPathComponent(path)
                        try content.write(to: finalPath, atomically: false, encoding: .utf8)
                        
                        print("DS:Wrote '\(content)' to \(finalPath.absoluteString)")
                        
                        self.SendUdp(connection: connection, to: sourceName, message: "return|create_file")
                    } catch {
                        print("DS:Error writing to '\(finalPath.absoluteString)': \(error)")
                    }
                    break
                case "crash":
                    fatalError("Simulated crash")
                    break
                default:
                    print("DS:Unrecognized command: \(control) m: \(message)")
                    break
                }
            }
            self.ReceiveBroadcast(on: connection)
        }
    }
    
    private func SendState(connection: NWConnection, to: String) {
        var payload: [String: Any] = [:]
            //"rest_url": NeftaPlugin._rtbUrl,
        //]
        
        if let NeftaInstance = NeftaPlugin._instance {
            payload["app_id"] = NeftaInstance._info._appId ?? ""
            payload["nuid"] = NeftaInstance._state._nuid
        }
        
        do {
            let jsonData = try JSONSerialization.data(withJSONObject: payload, options: [])
            SendUdp(connection: connection, to: to, message: "state|ios|\(_localPort)|\(_bundleId!)|\(self._version!)|\(_lastLogTime)|\(String(data:jsonData, encoding: .utf8)!)")
        } catch _ as NSError {
            
        }
    }
    
    private func ToProgressionStatus(_ name: String) -> NeftaEvents.ProgressionStatus {
        switch name {
            case "start":
                return NeftaEvents.ProgressionStatus.Start
            case "complete":
                return NeftaEvents.ProgressionStatus.Complete
            default:
                return NeftaEvents.ProgressionStatus.Fail
        }
    }
    
    private func ToProgressionType(_ name: String) -> NeftaEvents.ProgressionType {
        switch name {
            case "achievement":
                return .Achievement
            case "gameplay_unit":
                return .GameplayUnit
            case "item_level":
                return .ItemLevel
            case "unlock":
                return .Unlock
            case "player_level":
                return .PlayerLevel
            case "task":
                return .Task
            default:
                return .Other
        }
    }
    
    private func ToProgressionSource(_ name: String?) -> NeftaEvents.ProgressionSource {
        switch name {
            case nil:
                return NeftaEvents.ProgressionSource.Undefined
            case "core_content":
                return NeftaEvents.ProgressionSource.CoreContent
            case "optional_content":
                return NeftaEvents.ProgressionSource.OptionalContent
            case "boss":
                return NeftaEvents.ProgressionSource.Boss
            case "social":
                return NeftaEvents.ProgressionSource.Social
            case "special_event":
                return NeftaEvents.ProgressionSource.SpecialEvent
            default:
                return NeftaEvents.ProgressionSource.Other
        }
    }
    
    private func ToResourceCategory(_ name: String) -> NeftaEvents.ResourceCategory {
        switch name {
            case "soft_currency":
                return NeftaEvents.ResourceCategory.SoftCurrency
            case "premium_currency":
                return NeftaEvents.ResourceCategory.PremiumCurrency
            case "resource":
                return NeftaEvents.ResourceCategory.Resource
            case "consumable":
                return NeftaEvents.ResourceCategory.Consumable
            case "cosmetic_item":
                return NeftaEvents.ResourceCategory.CosmeticItem
            case "core_item":
                return NeftaEvents.ResourceCategory.CoreItem
            case "chest":
                return NeftaEvents.ResourceCategory.Chest
            case "experience":
                return NeftaEvents.ResourceCategory.Experience
            default:
                return NeftaEvents.ResourceCategory.Other
        }
    }
    
    private func ToReceiveMethod(_ name: String?) -> NeftaEvents.ReceiveMethod {
        switch name {
            case nil:
                return NeftaEvents.ReceiveMethod.Undefined
            case "level_end":
                return NeftaEvents.ReceiveMethod.LevelEnd
            case "reward":
                return NeftaEvents.ReceiveMethod.Reward
            case "loot":
                return NeftaEvents.ReceiveMethod.Loot
            case "shop":
                return NeftaEvents.ReceiveMethod.Shop
            case "iap":
                return NeftaEvents.ReceiveMethod.Iap
            case "create":
                return NeftaEvents.ReceiveMethod.Create
            default:
                return NeftaEvents.ReceiveMethod.Other
        }
    }
    
    private func ToSpendMethod(_ name: String?) -> NeftaEvents.SpendMethod {
        switch name {
            case nil:
                return NeftaEvents.SpendMethod.Undefined
            case "boost":
                return NeftaEvents.SpendMethod.Boost
            case "continuity":
                return NeftaEvents.SpendMethod.Continuity
            case "create":
                return NeftaEvents.SpendMethod.Create
            case "unlock":
                return NeftaEvents.SpendMethod.Unlock
            case "upgrade":
                return NeftaEvents.SpendMethod.Upgrade
            case "shop":
                return NeftaEvents.SpendMethod.Shop
            default:
                return NeftaEvents.SpendMethod.Other
        }
    }
    
    func GetBroadcastAddress() -> String? {
#if targetEnvironment(simulator)
        return "255.255.255.255"
#else
        var address : String?

        var ifaddr : UnsafeMutablePointer<ifaddrs>?
        guard getifaddrs(&ifaddr) == 0 else { return nil }
        guard let firstAddr = ifaddr else { return nil }

        for ifptr in sequence(first: firstAddr, next: { $0.pointee.ifa_next }) {
            let interface = ifptr.pointee
            let addrFamily = interface.ifa_addr.pointee.sa_family
            if addrFamily == UInt8(AF_INET) || addrFamily == UInt8(AF_INET6) {
                let name = String(cString: interface.ifa_name)
                if  name == "en0" {
                    var hostname = [CChar](repeating: 0, count: Int(NI_MAXHOST))
                    getnameinfo(interface.ifa_addr, socklen_t(interface.ifa_addr.pointee.sa_len),
                                &hostname, socklen_t(hostname.count),
                                nil, socklen_t(0), NI_NUMERICHOST)
                    address = String(cString: hostname)
                }
            }
        }
        freeifaddrs(ifaddr)
        
        guard let address = address else {
            return nil
        }
        let lastDotRange = address.range(of: ".", options: .backwards)!
        let baseString = address[..<lastDotRange.upperBound]
        return baseString + "255"
#endif
    }
}
