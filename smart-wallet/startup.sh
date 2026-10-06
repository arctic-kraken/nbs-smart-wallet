#!/bin/sh

set -e

cp /etc/secrets/ob_iss_ca_cert.crt /usr/local/share/ca-certificates/ob_iss_ca_cert.crt

cp /etc/secrets/ob_root_ca_cert.crt /usr/local/share/ca-certificates/ob_root_ca_cert.crt

chmod 644 /usr/local/share/ca-certificates/ob_iss_ca_cert.crt

chmod 644 /usr/local/share/ca-certificates/ob_root_ca_cert.crt

update-ca-certificates

exec "$@"