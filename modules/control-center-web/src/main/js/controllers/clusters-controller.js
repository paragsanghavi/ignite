/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// Controller for Clusters screen.
consoleModule.controller('clustersController', function ($http, $timeout, $scope, $state, $controller,
    $common, $focus, $confirm, $clone, $table, $preview, $loading, $unsavedChangesGuard, igniteIncludeEventGroups) {
        $unsavedChangesGuard.install($scope);

        // Initialize the super class and extend it.
        angular.extend(this, $controller('save-remove', {$scope: $scope}));

        $scope.ui = $common.formUI();

        $scope.joinTip = $common.joinTip;
        $scope.getModel = $common.getModel;
        $scope.compactJavaName = $common.compactJavaName;
        $scope.widthIsSufficient = $common.widthIsSufficient;
        $scope.saveBtnTipText = $common.saveBtnTipText;
        $scope.panelExpanded = $common.panelExpanded;

        $scope.tableVisibleRow = $table.tableVisibleRow;

        $scope.tableSave = function (field, index, stopEdit) {
            switch (field.type) {
                case 'table-simple':
                    if ($table.tableSimpleSaveVisible(field, index))
                        return $table.tableSimpleSave($scope.tableSimpleValid, $scope.backupItem, field, index, stopEdit);

                    break;
            }

            return true;
        };

        $scope.tableReset = function (save) {
            var field = $table.tableField();

            if (!save || !$common.isDefined(field) || $scope.tableSave(field, $table.tableEditedRowIndex(), true)) {
                $table.tableReset();

                return true;
            }

            return false;
        };

        $scope.tableNewItem = function (field) {
            if ($scope.tableReset(true)) {
                if (field.type === 'typeConfigurations') {
                    var binary = $scope.backupItem.binaryConfiguration;

                    if (!binary)
                        $scope.backupItem.binaryConfiguration = {typeConfigurations: [{}]};
                    else if (!$common.isDefined(binary.typeConfigurations))
                        binary.typeConfigurations = [{}];
                    else
                        binary.typeConfigurations.push({});
                }
                else
                    $table.tableNewItem(field);
            }
        };
        $scope.tableNewItemActive = $table.tableNewItemActive;

        $scope.tableStartEdit = function (item, field, index) {
            if ($scope.tableReset(true))
                $table.tableStartEdit(item, field, index);
        };
        $scope.tableEditing = $table.tableEditing;

        $scope.tableRemove = function (item, field, index) {
            if ($scope.tableReset(true)) {
                if (field.type === 'typeConfigurations')
                    $scope.backupItem.binaryConfiguration.typeConfigurations.splice(index, 1);
                else
                    $table.tableRemove(item, field, index);
            }
        };

        $scope.tableSimpleSave = $table.tableSimpleSave;
        $scope.tableSimpleSaveVisible = $table.tableSimpleSaveVisible;

        $scope.tableSimpleUp = function (item, field, index) {
            if ($scope.tableReset(true))
                $table.tableSimpleUp(item, field, index);
        };

        $scope.tableSimpleDown = function (item, field, index) {
            if ($scope.tableReset(true))
                $table.tableSimpleDown(item, field, index);
        };

        $scope.tableSimpleDownVisible = $table.tableSimpleDownVisible;

        $scope.tableEditedRowIndex = $table.tableEditedRowIndex;

        var previews = [];

        $scope.previewInit = function (preview) {
            previews.push(preview);

            $preview.previewInit(preview);
        };

        $scope.trustManagersConfigured = function() {
            return $scope.backupItem.sslEnabled && $common.isDefined($scope.backupItem.sslContextFactory) &&
                !$common.isEmptyArray($scope.backupItem.sslContextFactory.trustManagers);
        };

        $scope.previewChanged = $preview.previewChanged;

        $scope.hidePopover = $common.hidePopover;

        var showPopoverMessage = $common.showPopoverMessage;

        $scope.discoveries = [
            {value: 'Vm', label: 'static IPs'},
            {value: 'Multicast', label: 'multicast'},
            {value: 'S3', label: 'AWS S3'},
            {value: 'Cloud', label: 'apache jclouds'},
            {value: 'GoogleStorage', label: 'google cloud storage'},
            {value: 'Jdbc', label: 'JDBC'},
            {value: 'SharedFs', label: 'shared filesystem'}
        ];

        $scope.swapSpaceSpis = [
            {value: 'FileSwapSpaceSpi', label: 'File-based swap'},
            {value: undefined, label: 'Not set'}
        ];

        $scope.events = [];

        _.forEach(igniteIncludeEventGroups, function (evts, evtGrp) {
            $scope.events.push({label: evtGrp, value: evtGrp});
        });

        $scope.preview = {
            general: {xml: '', java: '', allDefaults: true},
            atomics: {xml: '', java: '', allDefaults: true},
            binary: {xml: '', java: '', allDefaults: true},
            communication: {xml: '', java: '', allDefaults: true},
            connector: {xml: '', java: '', allDefaults: true},
            deployment: {xml: '', java: '', allDefaults: true},
            discovery: {xml: '', java: '', allDefaults: true},
            events: {xml: '', java: '', allDefaults: true},
            igfs: {xml: '', java: '', allDefaults: true},
            marshaller: {xml: '', java: '', allDefaults: true},
            metrics: {xml: '', java: '', allDefaults: true},
            swap: {xml: '', java: '', allDefaults: true},
            time: {xml: '', java: '', allDefaults: true},
            pools: {xml: '', java: '', allDefaults: true},
            transactions: {xml: '', java: '', allDefaults: true},
            sslConfiguration: {xml: '', java: '', allDefaults: true}
        };

        $scope.cacheModes = $common.mkOptions(['LOCAL', 'REPLICATED', 'PARTITIONED']);

        $scope.deploymentModes = $common.mkOptions(['PRIVATE', 'ISOLATED', 'SHARED', 'CONTINUOUS']);

        $scope.transactionConcurrency = $common.mkOptions(['OPTIMISTIC', 'PESSIMISTIC']);

        $scope.transactionIsolation = $common.mkOptions(['READ_COMMITTED', 'REPEATABLE_READ', 'SERIALIZABLE']);

        $scope.segmentationPolicy = $common.mkOptions(['RESTART_JVM', 'STOP', 'NOOP']);

        $scope.marshallers = $common.mkOptions(['OptimizedMarshaller', 'JdkMarshaller', undefined]);

        $scope.sslKeyAlgorithms = ['SumX509', 'X509'];

        $scope.sslStoreType = ['JKS', 'PCKS11', 'PCKS12'];

        $scope.sslProtocols = ['TSL', 'SSL'];

        $scope.toggleExpanded = function () {
            $scope.ui.expanded = !$scope.ui.expanded;

            $common.hidePopover();
        };

        $scope.panels = {activePanels: [0]};

        var simpleTables = {
            addresses: {msg: 'Such IP address already exists!', id: 'IpAddress'},
            regions: {msg: 'Such region already exists!', id: 'Region'},
            zones: {msg: 'Such zone already exists!', id: 'Zone'},
            peerClassLoadingLocalClassPathExclude: {msg: 'Such package already exists!', id: 'PeerClsPathExclude'},
            trustManagers: {msg: 'Such trust manager already exists!', id: 'trustManagers'}
        };

        $scope.tableSimpleValid = function (item, field, val, index) {
            var model = $common.getModel(item, field)[field.model];

            if (field.model === 'trustManagers' && !$common.isValidJavaClass('Trust manager', val, false,  $table.tableFieldId(index, 'trustManagers'), false))
                return false;

            if ($common.isDefined(model)) {
                var idx = _.indexOf(model, val);

                // Found duplicate.
                if (idx >= 0 && idx !== index) {
                    var simpleTable = simpleTables[field.model];

                    if (simpleTable) {
                        $common.showPopoverMessage(null, null, $table.tableFieldId(index, simpleTable.id), simpleTable.msg);

                        return $table.tableFocusInvalidField(index, simpleTable.id);
                    }
                }
            }

            return true;
        };

        $scope.clusters = [];

        function _clusterLbl (cluster) {
            return cluster.name + ', ' + _.find($scope.discoveries, {value: cluster.discovery.kind}).label;
        }

        function selectFirstItem() {
            if ($scope.clusters.length > 0)
                $scope.selectItem($scope.clusters[0]);
        }

        $loading.start('loadingClustersScreen');

        // When landing on the page, get clusters and show them.
        $http.post('/api/v1/configuration/clusters/list')
            .success(function (data) {
                $scope.spaces = data.spaces;

                data.clusters.forEach(function (cluster) {
                    cluster.label = _clusterLbl(cluster);
                });

                $scope.clusters = data.clusters;

                $scope.caches = _.map(data.caches, function (cache) {
                    return {value: cache._id, label: cache.name, cache: cache};
                });

                $scope.igfss = _.map(data.igfss, function (igfs) {
                    return {value: igfs._id, label: igfs.name, igfs: igfs};
                });

                // Load page descriptor.
                $http.get('/models/clusters.json')
                    .success(function (data) {
                        $scope.screenTip = data.screenTip;
                        $scope.moreInfo = data.moreInfo;
                        $scope.general = data.general;
                        $scope.advanced = data.advanced;

                        $scope.ui.addGroups(data.general, data.advanced);

                        if ($common.getQueryVariable('new'))
                            $scope.createItem($common.getQueryVariable('id'));
                        else {
                            var lastSelectedCluster = angular.fromJson(sessionStorage.lastSelectedCluster);

                            if (lastSelectedCluster) {
                                var idx = _.findIndex($scope.clusters, function (cluster) {
                                    return cluster._id === lastSelectedCluster;
                                });

                                if (idx >= 0)
                                    $scope.selectItem($scope.clusters[idx]);
                                else {
                                    sessionStorage.removeItem('lastSelectedCluster');

                                    selectFirstItem();
                                }
                            }
                            else
                                selectFirstItem();
                        }

                        $scope.$watch('backupItem', function (val) {
                            if (val) {
                                var clusterCaches = _.reduce($scope.caches, function(caches, cache){
                                    if (_.contains(val.caches, cache.value)) {
                                        caches.push(cache.cache);
                                    }

                                    return caches;
                                }, []);

                                var srcItem = $scope.selectedItem ? $scope.selectedItem : prepareNewItem();

                                var igfss = _.map(_.filter($scope.igfss, function(igfs) {
                                    return _.indexOf(val.igfss, igfs.value) >= 0;
                                }), function(igfs) {
                                    return igfs.igfs;
                                });

                                $scope.ui.checkDirty(val, srcItem);

                                $scope.preview.general.xml = $generatorXml.clusterCaches(clusterCaches, null, true, $generatorXml.clusterGeneral(val)).asString();
                                $scope.preview.general.java = $generatorJava.clusterCaches(clusterCaches, null, true, $generatorJava.clusterGeneral(val)).asString();
                                $scope.preview.general.allDefaults = $common.isEmptyString($scope.preview.general.xml);

                                $scope.preview.atomics.xml = $generatorXml.clusterAtomics(val).asString();
                                $scope.preview.atomics.java = $generatorJava.clusterAtomics(val).asString();
                                $scope.preview.atomics.allDefaults = $common.isEmptyString($scope.preview.atomics.xml);

                                $scope.preview.binary.xml = $generatorXml.clusterBinary(val).asString();
                                $scope.preview.binary.java = $generatorJava.clusterBinary(val).asString();
                                $scope.preview.binary.allDefaults = $common.isEmptyString($scope.preview.binary.xml);

                                $scope.preview.communication.xml = $generatorXml.clusterCommunication(val).asString();
                                $scope.preview.communication.java = $generatorJava.clusterCommunication(val).asString();
                                $scope.preview.communication.allDefaults = $common.isEmptyString($scope.preview.communication.xml);

                                $scope.preview.connector.xml = $generatorXml.clusterConnector(val).asString();
                                $scope.preview.connector.java = $generatorJava.clusterConnector(val).asString();
                                $scope.preview.connector.allDefaults = $common.isEmptyString($scope.preview.connector.xml);

                                $scope.preview.deployment.xml = $generatorXml.clusterDeployment(val).asString();
                                $scope.preview.deployment.java = $generatorJava.clusterDeployment(val).asString();
                                $scope.preview.deployment.allDefaults = $common.isEmptyString($scope.preview.deployment.xml);

                                $scope.preview.discovery.xml = $generatorXml.clusterDiscovery(val.discovery).asString();
                                $scope.preview.discovery.java = $generatorJava.clusterDiscovery(val.discovery).asString();
                                $scope.preview.discovery.allDefaults = $common.isEmptyString($scope.preview.discovery.xml);

                                $scope.preview.events.xml = $generatorXml.clusterEvents(val).asString();
                                $scope.preview.events.java = $generatorJava.clusterEvents(val).asString();
                                $scope.preview.events.allDefaults = $common.isEmptyString($scope.preview.events.xml);

                                $scope.preview.igfs.xml = $generatorXml.igfss(igfss).asString();
                                $scope.preview.igfs.java = $generatorJava.igfss(igfss, 'cfg').asString();
                                $scope.preview.igfs.allDefaults = $common.isEmptyString($scope.preview.igfs.xml);

                                $scope.preview.marshaller.xml = $generatorXml.clusterMarshaller(val).asString();
                                $scope.preview.marshaller.java = $generatorJava.clusterMarshaller(val).asString();
                                $scope.preview.marshaller.allDefaults = $common.isEmptyString($scope.preview.marshaller.xml);

                                $scope.preview.metrics.xml = $generatorXml.clusterMetrics(val).asString();
                                $scope.preview.metrics.java = $generatorJava.clusterMetrics(val).asString();
                                $scope.preview.metrics.allDefaults = $common.isEmptyString($scope.preview.metrics.xml);

                                $scope.preview.swap.xml = $generatorXml.clusterSwap(val).asString();
                                $scope.preview.swap.java = $generatorJava.clusterSwap(val).asString();
                                $scope.preview.swap.allDefaults = $common.isEmptyString($scope.preview.swap.xml);

                                $scope.preview.time.xml = $generatorXml.clusterTime(val).asString();
                                $scope.preview.time.java = $generatorJava.clusterTime(val).asString();
                                $scope.preview.time.allDefaults = $common.isEmptyString($scope.preview.time.xml);

                                $scope.preview.pools.xml = $generatorXml.clusterPools(val).asString();
                                $scope.preview.pools.java = $generatorJava.clusterPools(val).asString();
                                $scope.preview.pools.allDefaults = $common.isEmptyString($scope.preview.pools.xml);

                                $scope.preview.transactions.xml = $generatorXml.clusterTransactions(val).asString();
                                $scope.preview.transactions.java = $generatorJava.clusterTransactions(val).asString();
                                $scope.preview.transactions.allDefaults = $common.isEmptyString($scope.preview.transactions.xml);

                                $scope.preview.sslConfiguration.xml = $generatorXml.clusterSsl(val).asString();
                                $scope.preview.sslConfiguration.java = $generatorJava.clusterSsl(val).asString();
                                $scope.preview.sslConfiguration.allDefaults = $common.isEmptyString($scope.preview.sslConfiguration.xml);
                            }
                        }, true);
                    })
                    .error(function (errMsg) {
                        $common.showError(errMsg);
                    });
            })
            .catch(function (errMsg) {
                $common.showError(errMsg);
            })
            .finally(function () {
                $scope.ui.ready = true;
                $loading.finish('loadingClustersScreen');
            });

        $scope.selectItem = function (item, backup) {
            function selectItem() {
                $table.tableReset();

                $scope.selectedItem = angular.copy(item);

                try {
                    if (item && item._id)
                        sessionStorage.lastSelectedCluster = angular.toJson(item._id);
                    else
                        sessionStorage.removeItem('lastSelectedCluster');
                }
                catch (error) { }

                _.forEach(previews, function(preview) {
                    preview.attractAttention = false;
                });

                if (backup)
                    $scope.backupItem = backup;
                else if (item)
                    $scope.backupItem = angular.copy(item);
                else
                    $scope.backupItem = undefined;

                if ($common.getQueryVariable('new'))
                    $state.go('base.configuration.clusters');
            }

            $common.confirmUnsavedChanges($scope.ui.isDirty(), selectItem);

            $scope.ui.formTitle = $common.isDefined($scope.backupItem) && $scope.backupItem._id ?
                'Selected cluster: ' + $scope.backupItem.name : 'New cluster';
        };

        function prepareNewItem(id) {
            var newItem = {
                discovery: {kind: 'Multicast', Vm: {addresses: ['127.0.0.1:47500..47510']}, Multicast: {}},
                deploymentMode: 'SHARED',
                binaryConfiguration: {
                    typeConfigurations: [],
                    compactFooter: true
                }
            };

            newItem.caches = id && _.find($scope.caches, {value: id}) ? [id] : [];
            newItem.igfss = id && _.find($scope.igfss, {value: id}) ? [id] : [];
            newItem.space = $scope.spaces[0]._id;

            return newItem;
        }

        // Add new cluster.
        $scope.createItem = function(id) {
            if ($scope.tableReset(true)) {
                $timeout(function () {
                    $common.ensureActivePanel($scope.panels, "general", 'clusterName');
                });

                $scope.selectItem(undefined, prepareNewItem(id));
            }
        };

        $scope.indexOfCache = function (cacheId) {
            return _.findIndex($scope.caches, function (cache) {
                return cache.value === cacheId;
            });
        };

        // Check cluster logical consistency.
        function validate(item) {
            if ($common.isEmptyString(item.name))
                return showPopoverMessage($scope.panels, 'general', 'clusterName', 'Name should not be empty');

            var caches = _.filter(_.map($scope.caches, function (scopeCache) {
                return scopeCache.cache;
            }), function (cache) {
                return _.contains($scope.backupItem.caches, cache._id);
            });

            var checkRes = $common.checkCachesDataSources(caches);

            if (!checkRes.checked) {
                return showPopoverMessage($scope.panels, 'general', 'caches',
                    'Found caches "' + checkRes.firstCache.name + '" and "' + checkRes.secondCache.name + '" ' +
                    'with the same data source bean name "' + checkRes.firstCache.cacheStoreFactory[checkRes.firstCache.cacheStoreFactory.kind].dataSourceBean +
                    '" and different configured databases: "' + $common.cacheStoreJdbcDialectsLabel(checkRes.firstDB) + '" in "' + checkRes.firstCache.name + '" and "' +
                    $common.cacheStoreJdbcDialectsLabel(checkRes.secondDB) + '" in "' + checkRes.secondCache.name + '"');
            }

            var b = item.binaryConfiguration;

            if ($common.isDefined(b)) {
                if (!$common.isEmptyString(b.idMapper) && !$common.isValidJavaClass('ID mapper', b.idMapper, false, 'idMapper', false, $scope.panels, 'binary')) {
                    $scope.ui.expanded = true;

                    return false;
                }

                if (!$common.isEmptyString(b.serializer) && !$common.isValidJavaClass('Serializer', b.serializer, false, 'serializer', false, $scope.panels, 'binary')) {
                    $scope.ui.expanded = true;

                    return false;
                }

                if (!$common.isEmptyArray(b.typeConfigurations)) {
                    var sameName = function (t, ix) {
                        return ix < typeIx && t.typeName === type.typeName;
                    };

                    for (var typeIx = 0; typeIx < b.typeConfigurations.length; typeIx++) {
                        var type = b.typeConfigurations[typeIx];

                        if ($common.isEmptyString(type.typeName)) {
                            $scope.ui.expanded = true;

                            showPopoverMessage($scope.panels, 'binary', 'typeName' + typeIx, 'Type name should be specified');

                            return false;
                        }

                        if (!$common.isEmptyString(type.typeName) && !$common.isValidJavaClass('Type name', type.typeName, false, 'typeName' + typeIx, false, $scope.panels, 'binary')) {
                            $scope.ui.expanded = true;

                            return false;
                        }

                        if (!$common.isEmptyString(type.idMapper) && !$common.isValidJavaClass('ID mapper', type.idMapper, false, 'idMapper' + typeIx, false, $scope.panels, 'binary')) {
                            $scope.ui.expanded = true;

                            return false;
                        }

                        if (!$common.isEmptyString(type.serializer) && !$common.isValidJavaClass('Serializer', type.serializer, false, 'serializer' + typeIx, false, $scope.panels, 'binary')) {
                            $scope.ui.expanded = true;

                            return false;
                        }

                        if (_.find(b.typeConfigurations, sameName)) {
                            $scope.ui.expanded = true;

                            showPopoverMessage($scope.panels, 'binary', 'typeName' + typeIx, 'Type with such name is already specified');

                            return false;
                        }
                    }
                }
            }

            var c = item.communication;

            if ($common.isDefined(c)) {
                if (!$common.isEmptyString(c.listener) && !$common.isValidJavaClass('Communication listener', c.listener, false, 'comListener', false, $scope.panels, 'communication')) {
                    $scope.ui.expanded = true;

                    return false;
                }

                if (!$common.isEmptyString(c.addressResolver) && !$common.isValidJavaClass('Address resolver', c.addressResolver, false, 'comAddressResolver', false, $scope.panels, 'communication')) {
                    $scope.ui.expanded = true;

                    return false;
                }

                if ($common.isDefined(c.unacknowledgedMessagesBufferSize)) {
                    if ($common.isDefined(c.messageQueueLimit))
                        if (c.unacknowledgedMessagesBufferSize < 5 * c.messageQueueLimit) {
                            $scope.ui.expanded = true;

                            showPopoverMessage($scope.panels, 'communication', 'unacknowledgedMessagesBufferSize', 'Maximum number of stored unacknowledged messages should be at least 5 * message queue limit');

                            return false;
                        }

                    if ($common.isDefined(c.ackSendThreshold))
                        if (c.unacknowledgedMessagesBufferSize < 5 * c.ackSendThreshold) {
                            $scope.ui.expanded = true;

                            showPopoverMessage($scope.panels, 'communication', 'unacknowledgedMessagesBufferSize', 'Maximum number of stored unacknowledged messages should be at least 5 * ack send threshold');

                            return false;
                        }
                }
            }

            var r = item.connector;

            if ($common.isDefined(r)) {
                if (!$common.isEmptyString(r.messageInterceptor) && !$common.isValidJavaClass('Message interceptor', r.messageInterceptor, false, 'connectorMessageInterceptor', false, $scope.panels, 'connector'))
                    return false;

                if (r.sslEnabled && $common.isEmptyString(r.sslFactory))
                    return showPopoverMessage($scope.panels, 'connector', 'connectorSslFactory', 'SSL factory should not be empty');

                if (r.sslEnabled && !$common.isEmptyString(r.sslFactory) && !$common.isValidJavaClass('SSL factory', r.sslFactory, false, 'connectorSslFactory', false, $scope.panels, 'connector'))
                    return false;
            }

            var d = item.discovery;

            if (!$common.isEmptyString(d.addressResolver) && !$common.isValidJavaClass('Address resolver', d.addressResolver, false, 'discoAddressResolver', false, $scope.panels, 'discovery'))
                return false;

            if (!$common.isEmptyString(d.listener) && !$common.isValidJavaClass('Discovery listener', d.listener, false, 'discoListener', false, $scope.panels, 'discovery'))
                return false;

            if (!$common.isEmptyString(d.dataExchange) && !$common.isValidJavaClass('Data exchange', d.dataExchange, false, 'dataExchange', false, $scope.panels, 'discovery'))
                return false;

            if (!$common.isEmptyString(d.metricsProvider) && !$common.isValidJavaClass('Metrics provider', d.metricsProvider, false, 'metricsProvider', false, $scope.panels, 'discovery'))
                return false;

            if (!$common.isEmptyString(d.authenticator) && !$common.isValidJavaClass('Node authenticator', d.authenticator, false, 'authenticator', false, $scope.panels, 'discovery'))
                return false;

            if (item.discovery.kind === 'Vm' && item.discovery.Vm.addresses.length === 0)
                return showPopoverMessage($scope.panels, 'general', 'addresses', 'Addresses are not specified');

            if (item.discovery.kind === 'S3' && $common.isEmptyString(item.discovery.S3.bucketName))
                return showPopoverMessage($scope.panels, 'general', 'bucketName', 'Bucket name should not be empty');

            if (item.discovery.kind === 'Cloud') {
                if ($common.isEmptyString(item.discovery.Cloud.identity))
                    return showPopoverMessage($scope.panels, 'general', 'identity', 'Identity should not be empty');

                if ($common.isEmptyString(item.discovery.Cloud.provider))
                    return showPopoverMessage($scope.panels, 'general', 'provider', 'Provider should not be empty');
            }

            if (item.discovery.kind === 'GoogleStorage') {
                if ($common.isEmptyString(item.discovery.GoogleStorage.projectName))
                    return showPopoverMessage($scope.panels, 'general', 'projectName', 'Project name should not be empty');

                if ($common.isEmptyString(item.discovery.GoogleStorage.bucketName))
                    return showPopoverMessage($scope.panels, 'general', 'bucketName', 'Bucket name should not be empty');

                if ($common.isEmptyString(item.discovery.GoogleStorage.serviceAccountP12FilePath))
                    return showPopoverMessage($scope.panels, 'general', 'serviceAccountP12FilePath', 'Private key path should not be empty');

                if ($common.isEmptyString(item.discovery.GoogleStorage.serviceAccountId))
                    return showPopoverMessage($scope.panels, 'general', 'serviceAccountId', 'Account ID should not be empty');
            }

            var swapKind = item.swapSpaceSpi.kind;

            if ($common.isDefined(swapKind)) {
                var sparsity = item.swapSpaceSpi[swapKind].maximumSparsity;

                if (sparsity < 0 || sparsity >= 1)
                    return showPopoverMessage($scope.panels, 'swap', 'maximumSparsity', 'Maximum sparsity should be more or equal 0 and less than 1');
            }

            if (item.sslEnabled) {
                if (!$common.isDefined(item.sslContextFactory) || $common.isEmptyString(item.sslContextFactory.keyStoreFilePath))
                    return showPopoverMessage($scope.panels, 'sslConfiguration', 'keyStoreFilePath', 'Key store file should not be empty');

                if ($common.isEmptyString(item.sslContextFactory.trustStoreFilePath) && $common.isEmptyArray(item.sslContextFactory.trustManagers))
                    return showPopoverMessage($scope.panels, 'sslConfiguration', 'sslConfiguration-title', 'Trust storage file or managers should be configured');
            }

            if (!item.swapSpaceSpi || !item.swapSpaceSpi.kind && item.caches) {
                for (var i = 0; i < item.caches.length; i++) {
                    var idx = $scope.indexOfCache(item.caches[i]);

                    if (idx >= 0) {
                        var cache = $scope.caches[idx];

                        if (cache.cache.swapEnabled) {
                            $scope.ui.expanded = true;

                            return showPopoverMessage($scope.panels, 'swap', 'swapSpaceSpi',
                                'Swap space SPI is not configured, but cache "' + cache.label + '" configured to use swap!');
                        }
                    }
                }
            }

            return true;
        }

        // Save cluster in database.
        function save(item) {
            $http.post('/api/v1/configuration/clusters/save', item)
                .success(function (_id) {
                    item.label = _clusterLbl(item);

                    $scope.ui.markPristine();

                    var idx = _.findIndex($scope.clusters, function (cluster) {
                        return cluster._id === _id;
                    });

                    if (idx >= 0)
                        angular.extend($scope.clusters[idx], item);
                    else {
                        item._id = _id;
                        $scope.clusters.push(item);
                    }

                    $scope.selectItem(item);

                    $common.showInfo('Cluster "' + item.name + '" saved.');
                })
                .error(function (errMsg) {
                    $common.showError(errMsg);
                });
        }

        // Save cluster.
        $scope.saveItem = function () {
            if ($scope.tableReset(true)) {
                var item = $scope.backupItem;

                if (validate(item))
                    save(item);
            }
        };

        function _clusterNames() {
            return _.map($scope.clusters, function (cluster) {
                return cluster.name;
            });
        }

        // Copy cluster with new name.
        $scope.cloneItem = function () {
            if ($scope.tableReset(true)) {
                if (validate($scope.backupItem))
                    $clone.confirm($scope.backupItem.name, _clusterNames()).then(function (newName) {
                        var item = angular.copy($scope.backupItem);

                        delete item._id;
                        item.name = newName;

                        save(item);
                    });
            }
        };

        // Remove cluster from db.
        $scope.removeItem = function () {
            $table.tableReset();

            var selectedItem = $scope.selectedItem;

            $confirm.confirm('Are you sure you want to remove cluster: "' + selectedItem.name + '"?')
                .then(function () {
                        var _id = selectedItem._id;

                        $http.post('/api/v1/configuration/clusters/remove', {_id: _id})
                            .success(function () {
                                $common.showInfo('Cluster has been removed: ' + selectedItem.name);

                                var clusters = $scope.clusters;

                                var idx = _.findIndex(clusters, function (cluster) {
                                    return cluster._id === _id;
                                });

                                if (idx >= 0) {
                                    clusters.splice(idx, 1);

                                    if (clusters.length > 0)
                                        $scope.selectItem(clusters[0]);
                                    else
                                        $scope.selectItem(undefined, undefined);
                                }
                            })
                            .error(function (errMsg) {
                                $common.showError(errMsg);
                            });
                });
        };

        // Remove all clusters from db.
        $scope.removeAllItems = function () {
            $table.tableReset();

            $confirm.confirm('Are you sure you want to remove all clusters?')
                .then(function () {
                        $http.post('/api/v1/configuration/clusters/remove/all')
                            .success(function () {
                                $common.showInfo('All clusters have been removed');

                                $scope.clusters = [];

                                $scope.selectItem(undefined, undefined);
                            })
                            .error(function (errMsg) {
                                $common.showError(errMsg);
                            });
                });
        };

        $scope.resetItem = function (group) {
            var resetTo = $scope.selectedItem;

            if (!$common.isDefined(resetTo))
                resetTo = prepareNewItem();

            $common.resetItem($scope.backupItem, resetTo, $scope.general, group);
            $common.resetItem($scope.backupItem, resetTo, $scope.advanced, group);
        };

        $scope.resetAll = function() {
            $table.tableReset();

            $confirm.confirm('Are you sure you want to undo all changes for current cluster?')
                .then(function() {
                    $scope.backupItem = $scope.selectedItem ? angular.copy($scope.selectedItem) : prepareNewItem();
                });
        };
    }
);
